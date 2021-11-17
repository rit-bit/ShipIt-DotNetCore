﻿using System;
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
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

        private readonly IStockRepository _stockRepository;
        private readonly IProductRepository _productRepository;

        public OutboundOrderController(IStockRepository stockRepository, IProductRepository productRepository)
        {
            _stockRepository = stockRepository;
            _productRepository = productRepository;
        }

        [HttpPost("")]
        public string
            Post([FromBody] OutboundOrderRequestModel request) // Start with a w_id and a list of orderLines (gtin + quantity)
        {
            Log.Info($"Processing outbound order: {request}");

            var gtins = new List<string>();
            foreach (var orderLine in request.OrderLines) // Create list of gtin strings
            {
                if (gtins.Contains(orderLine.gtin)) // Avoiding any duplicate gtins
                {
                    throw new ValidationException(
                        $"Outbound order request contains duplicate product gtin: {orderLine.gtin}");
                }

                gtins.Add(orderLine.gtin);
            }

            var productDataModels = _productRepository.GetProductsByGtin(gtins); // Get all the products for the gtins
            var products = productDataModels.ToDictionary(p => p.Gtin); // Make it a dictionary of gtin => product info

            var lineItems = new List<StockAlteration>();
            var productIds = new List<int>();
            var trucksPayload = new List<ProductOrder>();
            var errors = new List<string>();
            var totalOrderWeightInKgs = 0.0;

            foreach (var orderLine in request.OrderLines) // For every orderLine (gtin + quantity)
            {
                if (!products.ContainsKey(orderLine.gtin))
                {
                    errors.Add($"Unknown product gtin: {orderLine.gtin}");
                }
                else
                {
                    var product = products[orderLine.gtin];
                    var weightInKgs = product.WeightInGrams / 1000;
                    var quantity = orderLine.quantity;
                    totalOrderWeightInKgs += weightInKgs * quantity; // Increase totalOrderWeight
                    lineItems.Add(new StockAlteration(product.Id, quantity)); // Take note of how much to decrease stock by
                    productIds.Add(product.Id); // Record product id
                    trucksPayload.Add(new ProductOrder(new Product(product), quantity));
                }
            }

            var trucksNeeded = Math.Ceiling(totalOrderWeightInKgs / Truck.MaxWeightInKgs);
            var trucksNeededString =
                $"For this outbound order weighing {totalOrderWeightInKgs}kg, at least {trucksNeeded} truck(s) are needed.";

            if (errors.Count > 0)
            {
                throw new NoSuchEntityException(string.Join("; ", errors));
            }

            var stock = _stockRepository.GetStockByWarehouseAndProductIds(request.WarehouseId,
                productIds); // Get stock levels for relevant productIDs

            var orderLines = request.OrderLines.ToList();
            errors = new List<string>();

            for (var i = 0; i < lineItems.Count; i++) // For every stock alteration we need to do
            {
                var lineItem = lineItems[i];
                var orderLine = orderLines[i]; // Get the corresponding orderLine (gtin + quantity)

                if (!stock.ContainsKey(lineItem.ProductId))
                {
                    errors.Add($"Product: {orderLine.gtin}, no stock held");
                    continue;
                }

                var item = stock[lineItem.ProductId]; // and check if we have enough stock
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

            _stockRepository.RemoveStock(request.WarehouseId, lineItems); // If all items are sufficiently in stock, adjust stock levels accordingly

            var truckLoadingDetails = TruckLoader.CalculateTruckLoadingDetails(trucksPayload);
            return trucksNeededString + Environment.NewLine + truckLoadingDetails;
        }
    }
}