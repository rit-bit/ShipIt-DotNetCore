using System.Collections.Generic;
using System.Linq;
using Npgsql;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Models.DataModels;

namespace ShipIt.Repositories
{
    public interface IStockRepository
    {
        int GetTrackedItemsCount();
        int GetStockHeldSum();
        IEnumerable<CompanyProductStockDataModel> GetCompanyProductStockByWarehouseId(int id);
        IEnumerable<StockDataModel> GetStockByWarehouseId(int id);
        Dictionary<int, StockDataModel> GetStockByWarehouseAndProductIds(int warehouseId, List<int> productIds);
        void RemoveStock(int warehouseId, List<StockAlteration> lineItems);
        void AddStock(int warehouseId, List<StockAlteration> lineItems);
    }

    public class StockRepository : RepositoryBase, IStockRepository
    {

        public int GetTrackedItemsCount()
        {
            const string sql = "SELECT COUNT(*) FROM stock";
            return (int)QueryForLong(sql);
        }

        public int GetStockHeldSum()
        {
            const string sql = "SELECT SUM(hld) FROM stock";
            return (int)QueryForLong(sql);
        }

        public IEnumerable<CompanyProductStockDataModel> GetCompanyProductStockByWarehouseId(int id) {
            const string sql = "SELECT * FROM stock JOIN gtin ON stock.p_id = gtin.p_id " + 
                               "JOIN gcp ON gtin.gcp_cd = gcp.gcp_cd WHERE w_id = @w_id"; // TODO LEFT OR INNER JOIN?
            var parameter = new NpgsqlParameter("@w_id", id);
            var noProductWithIdErrorMessage = $"No stock found with w_id: {id}";
            try
            {
                return RunGetQuery(sql, reader => new CompanyProductStockDataModel(reader), 
                    noProductWithIdErrorMessage, parameter).ToList();
            }
            catch (NoSuchEntityException)
            {
                return new List<CompanyProductStockDataModel>();
            } 
        }

        public IEnumerable<StockDataModel> GetStockByWarehouseId(int id)
        {
            const string sql = "SELECT p_id, hld, w_id FROM stock WHERE w_id = @w_id";
            var parameter = new NpgsqlParameter("@w_id", id);
            var noProductWithIdErrorMessage = $"No stock found with w_id: {id}";
            try
            {
                return RunGetQuery(sql, reader => new StockDataModel(reader), 
                    noProductWithIdErrorMessage, parameter).ToList();
            }
            catch (NoSuchEntityException)
            {
                return new List<StockDataModel>();
            }
        }

        public Dictionary<int, StockDataModel> GetStockByWarehouseAndProductIds(int warehouseId, List<int> productIds)
        {
            var sql = $"SELECT p_id, hld, w_id FROM stock WHERE w_id = @w_id AND p_id IN ({string.Join(",", productIds)})";
            var parameter = new NpgsqlParameter("@w_id", warehouseId);
            var noProductWithIdErrorMessage = $"No stock found with w_id: {warehouseId} and p_ids: {string.Join(",", productIds)}";
            var stock = RunGetQuery(sql, reader => new StockDataModel(reader), noProductWithIdErrorMessage, parameter);
            return stock.ToDictionary(s => s.ProductId, s => s);
        }
            
        public void AddStock(int warehouseId, List<StockAlteration> lineItems)
        {
            var parametersList = new List<NpgsqlParameter[]>();
            foreach (var orderLine in lineItems)
            {
                parametersList.Add(
                    new[] {
                        new NpgsqlParameter("@p_id", orderLine.ProductId),
                        new NpgsqlParameter("@w_id", warehouseId),
                        new NpgsqlParameter("@hld", orderLine.Quantity)
                    }
                );
            }

            const string sql = "INSERT INTO stock (p_id, w_id, hld) VALUES (@p_id, @w_id, @hld) "
                               + "ON CONFLICT (p_id, w_id) DO UPDATE SET hld = stock.hld + EXCLUDED.hld";

            var recordsAffected = new List<int>();
            foreach (var parameters in parametersList)
            {
                 recordsAffected.Add(
                     RunSingleQueryAndReturnRecordsAffected(sql, parameters)
                 );
            }

            string errorMessage = null;

            for (var i = 0; i < recordsAffected.Count; i++)
            {
                if (recordsAffected[i] == 0)
                {
                    errorMessage = $"Product {parametersList[i][0]} in warehouse {warehouseId} was unexpectedly not updated "
                                + "(rows updated returned {recordsAffected[i]})";
                }
            }

            if (errorMessage != null)
            {
                throw new InvalidStateException(errorMessage);
            }
        }

        public void RemoveStock(int warehouseId, List<StockAlteration> lineItems)
        {
            var sql = $"UPDATE stock SET hld = hld - @hld WHERE w_id = {warehouseId} AND p_id = @p_id";

            var parametersList = new List<NpgsqlParameter[]>();
            foreach (var lineItem in lineItems)
            {
                parametersList.Add(new[]
                {
                    new NpgsqlParameter("@hld", lineItem.Quantity),
                    new NpgsqlParameter("@p_id", lineItem.ProductId)
                });
            }

            RunTransaction(sql, parametersList);
        }
    }
}