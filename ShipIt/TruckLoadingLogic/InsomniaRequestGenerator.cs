using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ShipIt.Models.ApiModels;
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
            for (var i = 0; i < numberOfProducts; i++)
            {
                var index = rand.Next(companyProductStockModels.Count);
                var companyProductStockModel = companyProductStockModels[index];
                companyProductStockModels.Remove(companyProductStockModel);
                
                var stockDataModel = companyProductStockModel.StockDataModel;
                var productDataModel = companyProductStockModel.ProductDataModel;
                var companyDataModel = companyProductStockModel.CompanyDataModel;
                var gtin = productDataModel.Gtin;
                var quantity = rand.Next(stockDataModel.held);
                var orderLine = new OrderLine()
                {
                    gtin = gtin,
                    quantity = quantity
                };
                orderLines.Add(orderLine);
            }
            

            return JsonConvert.SerializeObject(new OutboundOrderRequestModel
            {
                WarehouseId = warehouseId,
                OrderLines = orderLines
            });
        }
    }
}