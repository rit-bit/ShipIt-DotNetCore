using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ShipIt.Models.ApiModels;
using ShipIt.Models.DataModels;
using ShipIt.Repositories;

namespace ShipIt.TruckLoadingLogic
{
    public class InsomniaRequestGenerator
    {
        private readonly IStockRepository _stockRepo;

        public InsomniaRequestGenerator(IStockRepository stockRepo)
        {
            _stockRepo = stockRepo;
        }
        
        public string GenerateRandomRequest(int numberOfProducts, int warehouseId)
        {
            var rand = new Random();
            var orderLines = new List<OrderLine>();
            var companyProductStockModels = _stockRepo
                .GetCompanyProductStockByWarehouseId(warehouseId).ToList();

            if (numberOfProducts > companyProductStockModels.Count)
            {
                throw new ArgumentException($"Requested {numberOfProducts} products, " +
                                            $"but there are only {companyProductStockModels.Count} in the dataset.");
            }
            
            orderLines.AddRange(GenerateRandomOrderLines(numberOfProducts, companyProductStockModels));

            return JsonConvert.SerializeObject(new OutboundOrderRequestModel
            {
                WarehouseId = warehouseId,
                OrderLines = orderLines
            });
        }

        private static IEnumerable<OrderLine> GenerateRandomOrderLines(int numberOfProducts, IList<CompanyProductStockDataModel> listOfData)
        {
            var rand = new Random();
            var availableIndices = Enumerable.Range(0, listOfData.Count).ToList();
            var indicesToUse = availableIndices.OrderBy(x => rand.Next()).Take(numberOfProducts);

            foreach (var index in indicesToUse)
            {
                var data = listOfData[index];
                listOfData.Remove(data);
                
                var stockDataModel = data.StockDataModel;
                var productDataModel = data.ProductDataModel;
                var companyDataModel = data.CompanyDataModel;
                var gtin = productDataModel.Gtin;
                var quantity = rand.Next(stockDataModel.held);
                var orderLine = new OrderLine()
                {
                    gtin = gtin,
                    quantity = quantity
                };
                yield return orderLine;
            }
        }
    }
}