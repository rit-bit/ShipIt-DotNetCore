using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShipIt.Models.ApiModels;

namespace ShipIt.TruckLoadingLogic
{
    public static class TruckLoader
    {
        private static List<Truck> Trucks { get; } = new List<Truck> {new()};

        public static string CalculateTruckLoadingDetails(List<ProductOrder> productOrders) // TODO Test this works
        {
            // TODO First go through the list and split any productOrders that are > Truck.MaxWeightInKgs into smaller productOrders
            
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

        private static string StringifyTruckLoads()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"This order requires {Trucks.Count} trucks.");
            for (var i = 0; i < Trucks.Count; i++)
            {
                var truck = Trucks[i];
                stringBuilder.AppendLine($"For truck #{i + 1}, load the following:");
                stringBuilder.AppendLine(truck.StringifyLoad());
                stringBuilder.AppendLine();
            }

            return stringBuilder.ToString();
        }
    }
}