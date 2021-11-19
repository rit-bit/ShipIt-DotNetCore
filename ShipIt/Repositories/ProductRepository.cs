using System.Collections.Generic;
using System.Linq;
using Npgsql;
using ShipIt.Exceptions;
using ShipIt.Models.DataModels;

namespace ShipIt.Repositories
{
    public interface IProductRepository
    {
        int GetCount();
        ProductDataModel GetProductByGtin(string gtin);
        IEnumerable<ProductDataModel> GetProductsByGtin(IEnumerable<string> gtins);
        ProductDataModel GetProductById(int id);
        void AddProducts(IEnumerable<ProductDataModel> products);
        void DiscontinueProductByGtin(string gtin);
    }

    public class ProductRepository : RepositoryBase, IProductRepository
    {
        public int GetCount()
        {
            const string employeeCountSql = "SELECT COUNT(*) FROM gcp";
            return (int) QueryForLong(employeeCountSql);
        }

        public ProductDataModel GetProductByGtin(string gtin)
        {
            const string sql = "SELECT p_id, gtin_cd, gcp_cd, gtin_nm, m_g, l_th, ds, min_qt FROM gtin WHERE gtin_cd = @gtin_cd";
            var parameter = new NpgsqlParameter("@gtin_cd", gtin);
            return RunSingleGetQuery(sql, reader => new ProductDataModel(reader),
                $"No products found with gtin of value {gtin}", parameter);
        }

        public IEnumerable<ProductDataModel> GetProductsByGtin(IEnumerable<string> gtins)
        {
            var sql = $"SELECT p_id, gtin_cd, gcp_cd, gtin_nm, m_g, l_th, ds, min_qt " +
                      $"FROM gtin WHERE gtin_cd IN ('{string.Join("','", gtins)}')";
            return RunGetQuery(sql, reader => new ProductDataModel(reader), 
                "No products found with given gtin ids", null);
        }

        public ProductDataModel GetProductById(int id)
        {
            const string sql = "SELECT p_id, gtin_cd, gcp_cd, gtin_nm, m_g, l_th, ds, min_qt FROM gtin WHERE p_id = @p_id";
            var parameter = new NpgsqlParameter("@p_id", id);
            var noProductWithIdErrorMessage = $"No products found with id of value {id}";
            return RunSingleGetQuery(sql, reader => new ProductDataModel(reader), noProductWithIdErrorMessage, parameter);
        }

        public void DiscontinueProductByGtin(string gtin)
        {
            const string sql = "UPDATE gtin SET ds = 1 WHERE gtin_cd = @gtin_cd";
            var parameter = new NpgsqlParameter("@gtin_cd", gtin);
            var noProductWithGtinErrorMessage =
                $"No products found with gtin of value {gtin}";

            RunSingleQuery(sql, noProductWithGtinErrorMessage, parameter);
        }

        public void AddProducts(IEnumerable<ProductDataModel> products)
        {
            const string sql = "INSERT INTO gtin (gtin_cd, gcp_cd, gtin_nm, m_g, l_th, ds, min_qt) " + 
                               "VALUES (@gtin_cd, @gcp_cd, @gtin_nm, @m_g, @l_th, @ds, @min_qt)";

            var parametersList = new List<NpgsqlParameter[]>();
            var gtins = new List<string>();

            foreach (var product in products)
            {
                if (gtins.Contains(product.Gtin))
                {
                    throw new MalformedRequestException($"Cannot add products with duplicate gtins: {product.Gtin}");
                }
                gtins.Add(product.Gtin);
                parametersList.Add(product.GetNpgsqlParameters().ToArray());
            }

            var conflicts = TryGetProductsByGtin(gtins).ToList();
            if (conflicts.Any())
            {
                throw new MalformedRequestException(
                    $"Cannot add products with existing gtins: {string.Join(", ", conflicts.Select(c => c.Gtin))}");
            }

            RunTransaction(sql, parametersList);
        }

        private IEnumerable<ProductDataModel> TryGetProductsByGtin(List<string> gtins)
        {
            try
            {
                var products = GetProductsByGtin(gtins).ToList();
                return products;
            }
            catch (NoSuchEntityException)
            {
                return new List<ProductDataModel>();
            }
        }
    }
}