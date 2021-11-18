using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Repositories;
using ShipIt.TruckLoadingLogic;

namespace ShipIt.Controllers
{
    [Route("orders/outbound")]
    public class OutboundOrderController : ControllerBase
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

        private readonly IStockRepository _stockRepository;
        private readonly IProductRepository _productRepository;

        public OutboundOrderController(IStockRepository stockRepository, IProductRepository productRepository)
        {
            _stockRepository = stockRepository;
            _productRepository = productRepository;
        }

        [HttpGet("")]
        public string Get([FromQuery] int numberOfItems, [FromQuery] int warehouseId)
        {
            var requestGenerator = new InsomniaRequestGenerator(_stockRepository);
            return requestGenerator.GenerateRandomRequest(numberOfItems, warehouseId);
        }

        [HttpPost("")]
        public string Post([FromBody] OutboundOrderRequestModel request)
        {
            // Start with a w_id and a list of orderLines (gtin + quantity)
            Log.Info($"Processing outbound order: {request}");

            var gtins = GetValidatedGtins(request);

            // Get all the products for the gtins
            var productDataModels = _productRepository.GetProductsByGtin(gtins);
            // Make it a dictionary of gtin => product info
            var productDataModelsDict = productDataModels.ToDictionary(p => p.Gtin);

            var lineItems = new List<StockAlteration>();
            var productIds = new List<int>();
            var trucksPayload = new List<ProductOrder>();
            var errors = new List<string>();

            // For every orderLine (gtin + quantity)
            foreach (var orderLine in request.OrderLines)
            {
                if (!productDataModelsDict.ContainsKey(orderLine.gtin))
                {
                    errors.Add($"Unknown product gtin: {orderLine.gtin}");
                }
                else
                {
                    var product = productDataModelsDict[orderLine.gtin];
                    var quantity = orderLine.quantity;
                    // Take note of how much to decrease stock by
                    lineItems.Add(new StockAlteration(product.Id, quantity));
                    // Record product id
                    productIds.Add(product.Id);
                    trucksPayload.Add(new ProductOrder(new Product(product), quantity));
                }
            }

            if (errors.Count > 0)
            {
                throw new NoSuchEntityException(string.Join("; ", errors));
            }

            // Get stock levels for relevant productIDs
            var stock = _stockRepository.GetStockByWarehouseAndProductIds(request.WarehouseId, productIds);

            var orderLines = request.OrderLines.ToList();
            errors = new List<string>();

            // For every stock alteration we need to do
            for (var i = 0; i < lineItems.Count; i++)
            {
                var lineItem = lineItems[i];
                // Get the corresponding orderLine (gtin + quantity)
                var orderLine = orderLines[i];

                if (!stock.ContainsKey(lineItem.ProductId))
                {
                    errors.Add($"Product: {orderLine.gtin}, no stock held");
                    continue;
                }

                // and check if we have enough stock
                var item = stock[lineItem.ProductId];
                if (lineItem.Quantity > item.held)
                {
                    errors.Add(
                        $"Product: {orderLine.gtin}, stock held: {item.held}, stock to remove: {lineItem.Quantity}");
                }
            }

            if (errors.Count > 0)
            {
                throw new InsufficientStockException(string.Join("; ", errors));
            }

            // If all items are sufficiently in stock, adjust stock levels accordingly
            _stockRepository.RemoveStock(request.WarehouseId, lineItems);

            return TruckLoader.CalculateTruckLoadingDetails(trucksPayload);
        }

        private static IEnumerable<string> GetValidatedGtins(OutboundOrderRequestModel request)
        {
            var gtins = new List<string>();
            // Create list of gtin strings
            foreach (var orderLine in request.OrderLines)
            {
                // Avoiding any duplicate gtins
                if (gtins.Contains(orderLine.gtin))
                {
                    throw new ValidationException(
                        $"Outbound order request contains duplicate product gtin: {orderLine.gtin}");
                }

                gtins.Add(orderLine.gtin);
            }

            return gtins;
        }
    }
}