using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShipIt.TruckLoadingLogic
{
    public static class TruckLoader
    {
        private static List<Truck> Trucks { get; } = new List<Truck> {new()};

        public static string CalculateTruckLoadingDetails(IEnumerable<ProductOrder> productOrders) // TODO Test this works
        {
            // Split any productOrders that are > Truck.MaxWeightInKgs into smaller productOrders
            productOrders = SplitLargeOrderLines(productOrders);
            
            // Sort list to have largest weighted productOrders first
            productOrders = productOrders.OrderBy(productOrder => productOrder.OrderWeightInKgs).ToList();
            
            // Then for each productOrder in the list, see if it will fit on an existing truck, or add a new truck if not
            foreach (var productOrder in productOrders)
            {
                var productOrderHasBeenLoaded = false;
                foreach (var truck in Trucks)
                {
                    if (truck.HasRoomFor(productOrder))
                    {
                        truck.Add(productOrder);
                        productOrderHasBeenLoaded = true;
                        break;
                    }
                }

                if (!productOrderHasBeenLoaded)
                {
                    var newTruck = new Truck();
                    newTruck.Add(productOrder);
                    Trucks.Add(newTruck);
                }
            }

            return StringifyTruckLoads();
        }

        private static IEnumerable<ProductOrder> SplitLargeOrderLines(IEnumerable<ProductOrder> productOrders)
        {
            var newProductOrders = new List<ProductOrder>();
            // If productOrder weight is too much
            // Work out how many of that product fills a truck
            // Create X trucks full, plus one truck partly full if necessary
            foreach (var productOrder in productOrders)
                if (productOrder.OrderWeightInKgs > Truck.MaxWeightInKgs)
                {
                    {
                        var quantityPerTruck = (int) (Truck.MaxWeightInKgs / productOrder.Product.WeightInKgs);
                        var trucksNeeded = productOrder.Quantity / quantityPerTruck;
                        var quantityLeftToLoad = productOrder.Quantity % quantityPerTruck;

                        for (var i = 0; i < trucksNeeded; i++)
                        {
                            newProductOrders.Add(new ProductOrder(productOrder.Product, quantityPerTruck));
                        }

                        if (quantityLeftToLoad > 0)
                        {
                            newProductOrders.Add(new ProductOrder(productOrder.Product, quantityLeftToLoad));
                        }
                    }
                }
                else
                {
                    newProductOrders.Add(productOrder);
                }

            return newProductOrders;
        }

        private static string StringifyTruckLoads()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"This order requires {Trucks.Count} trucks.");
            stringBuilder.AppendLine();
            for (var i = 0; i < Trucks.Count; i++)
            {
                var truck = Trucks[i];
                stringBuilder.AppendLine($"For truck #{i + 1}, (total load {truck.TotalWeightInKgs}kg) load the following:");
                stringBuilder.AppendLine(truck.StringifyLoad());
            }

            return stringBuilder.ToString();
        }
    }
}