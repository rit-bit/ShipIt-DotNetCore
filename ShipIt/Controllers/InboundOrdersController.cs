using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Models.DataModels;
using ShipIt.Repositories;

namespace ShipIt.Controllers
{
    [Route("orders/inbound")]
    public class InboundOrderController : ControllerBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

        private readonly IEmployeeRepository _employeeRepository;
        private readonly IProductRepository _productRepository;
        private readonly IStockRepository _stockRepository;

        public InboundOrderController(IEmployeeRepository employeeRepository, IProductRepository productRepository, IStockRepository stockRepository)
        {
            _employeeRepository = employeeRepository;
            _stockRepository = stockRepository;
            _productRepository = productRepository;
        }

        [HttpGet("{warehouseId:int}")]
        public InboundOrderResponse Get([FromRoute] int warehouseId)
        {
            Log.Info($"orderIn for warehouseId: {warehouseId}");

            var operationsManager = new Employee(_employeeRepository.GetOperationsManager(warehouseId));

            Log.Debug($"Found operations manager: {operationsManager}");

            // var allStock = _stockRepository.GetStockByWarehouseId(warehouseId);
            var allStockWithProductAndCompanyInfo = _stockRepository.GetCompanyProductStockByWarehouseId(warehouseId);

            var orderLinesByCompany = new Dictionary<Company, List<InboundOrderLine>>();
            foreach (var dataModel in allStockWithProductAndCompanyInfo)
            {
                var stockDataModel = dataModel.StockDataModel;
                var product = new Product(dataModel.ProductDataModel);
                var company = new Company(dataModel.CompanyDataModel);

                if(stockDataModel.held < product.LowerThreshold && !product.Discontinued)
                {

                    var orderQuantity = Math.Max(product.LowerThreshold * 3 - stockDataModel.held, product.MinimumOrderQuantity);

                    if (!orderLinesByCompany.ContainsKey(company))
                    {
                        orderLinesByCompany.Add(company, new List<InboundOrderLine>());
                    }

                    orderLinesByCompany[company].Add( 
                        new InboundOrderLine()
                        {
                            gtin = product.Gtin,
                            name = product.Name,
                            quantity = orderQuantity
                        }
                    );
                }
            }

            Log.Debug($"Constructed order lines: {orderLinesByCompany}");

            var orderSegments = orderLinesByCompany.Select(ol => new OrderSegment()
            {
                OrderLines = ol.Value,
                Company = ol.Key
            });

            Log.Info("Constructed inbound order");

            return new InboundOrderResponse()
            {
                OperationsManager = operationsManager,
                WarehouseId = warehouseId,
                OrderSegments = orderSegments
            };
        }

        [HttpPost("")]
        public void Post([FromBody] InboundManifestRequestModel requestModel)
        {
            Log.Info("Processing manifest: " + requestModel);

            var gtins = new List<string>();

            foreach (var orderLine in requestModel.OrderLines)
            {
                if (gtins.Contains(orderLine.gtin))
                {
                    throw new ValidationException($"Manifest contains duplicate product gtin: {orderLine.gtin}");
                }
                gtins.Add(orderLine.gtin);
            }

            var productDataModels = _productRepository.GetProductsByGtin(gtins);
            var products = productDataModels.ToDictionary(p => p.Gtin, p => new Product(p));

            Log.Debug($"Retrieved products to verify manifest: {products}");

            var lineItems = new List<StockAlteration>();
            var errors = new List<string>();

            foreach (var orderLine in requestModel.OrderLines)
            {
                if (!products.ContainsKey(orderLine.gtin))
                {
                    errors.Add($"Unknown product gtin: {orderLine.gtin}");
                    continue;
                }

                var product = products[orderLine.gtin];
                if (!product.Gcp.Equals(requestModel.Gcp))
                {
                    errors.Add($"Manifest GCP ({requestModel.Gcp}) doesn't match Product GCP ({product.Gcp})");
                }
                else
                {
                    lineItems.Add(new StockAlteration(product.Id, orderLine.quantity));
                }
            }

            if (errors.Any())
            {
                Log.Debug($"Found errors with inbound manifest: {errors}");
                throw new ValidationException($"Found inconsistencies in the inbound manifest: {string.Join("; ", errors)}");
            }

            Log.Debug($"Increasing stock levels with manifest: {requestModel}");
            _stockRepository.AddStock(requestModel.WarehouseId, lineItems);
            Log.Info("Stock levels increased");
        }
    }
}
