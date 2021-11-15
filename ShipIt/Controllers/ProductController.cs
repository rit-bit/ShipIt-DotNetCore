﻿﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Models.DataModels;
using ShipIt.Parsers;
using ShipIt.Repositories;
using ShipIt.Validators;

namespace ShipIt.Controllers
{
    [Route("products")]
    public class ProductController : ControllerBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

        private readonly IProductRepository _productRepository;

        public ProductController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        [HttpGet("{gtin}")]
        public ProductResponse Get([FromRoute] string gtin)
        {
            if (gtin == null)
            {
                throw new MalformedRequestException("Unable to parse gtin from request parameters");
            }

            Log.Info("Looking up product by gtin: " + gtin);

            var product = new Product(_productRepository.GetProductByGtin(gtin));

            Log.Info("Found product: " + product);

            return new ProductResponse(product);
        }

        [HttpPost("")]
        public Response Post([FromBody] ProductsRequestModel requestModel)
        {
            var parsedProducts = new List<Product>(); // TODO Replace the following foreach loop with the new ParseAll method from the ProductParser class, then check it works

            foreach (var requestProduct in requestModel.Products)
            {
                var parsedProduct = requestProduct.Parse();
                new ProductValidator().Validate(parsedProduct);
                parsedProducts.Add(parsedProduct);
            }

            Log.Info("Adding products: " + parsedProducts);

            var dataProducts = parsedProducts.Select(p => new ProductDataModel(p));
            _productRepository.AddProducts(dataProducts);
            
            Log.Debug("Products added successfully");

            return new Response() { Success = true };
        }

        [HttpPatch("{gtin}")]
        public Response Discontinue([FromRoute] string gtin)
        {
            if (gtin == null)
            {
                throw new MalformedRequestException("Unable to parse gtin from request parameters");
            }

            Log.Info("Discontinuing up product by gtin: " + gtin);

            _productRepository.DiscontinueProductByGtin(gtin);

            Log.Info("Discontinued product: " + gtin);

            return new Response() { Success = true };
        }
    }
}
