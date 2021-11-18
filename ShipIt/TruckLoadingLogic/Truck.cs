using System;
using System.Collections.Generic;
using System.Text;
using ShipIt.Models.ApiModels;

namespace ShipIt.TruckLoadingLogic
{
    public class Truck
    {
        public const int MaxWeightInKgs = 2000;

        private readonly List<ProductOrder> _load = new();
        public float TotalWeightInKgs { get; private set; }

        public bool HasRoomFor(ProductOrder productOrder)
        {
            return MaxWeightInKgs - TotalWeightInKgs >= productOrder.Quantity * productOrder.Product.WeightInKgs;
        }

        public void Add(ProductOrder productOrder)
        {
            if (!HasRoomFor(productOrder))
            {
                throw new ArgumentException($"Truck is at capacity {TotalWeightInKgs} out of {MaxWeightInKgs} " +
                                            $"and does not have space for these items: {productOrder.Quantity}x GTIN:{productOrder.Product.Gtin}");
            }

            TotalWeightInKgs += productOrder.Product.WeightInKgs * productOrder.Quantity;
            _load.Add(productOrder);
        }

        public string StringifyLoad()
        {
            var stringBuilder = new StringBuilder();
            foreach (var productOrder in _load)
            {
                var product = productOrder.Product;
                stringBuilder.AppendLine($"    {productOrder.Quantity} x GTIN: {product.Gtin} ({product.Name}) " +
                                         $"  --  {product.WeightInKgs}kg x {productOrder.Quantity} = {product.WeightInKgs * productOrder.Quantity}kg");
            }

            return stringBuilder.ToString();
        }
    }

    public class ProductOrder
    {
        public Product Product { get; }
        public int Quantity { get; }
        public float OrderWeightInKgs { get; }

        public ProductOrder(Product product, int quantity)
        {
            Product = product;
            Quantity = quantity;
            OrderWeightInKgs = product.WeightInKgs * quantity;
        }
    }
}