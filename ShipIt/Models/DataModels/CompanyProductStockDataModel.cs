using System.Collections.Generic;

namespace ShipIt.Models.DataModels
{
    public class CompanyProductStockDataModel : DataModel
    {
        public IEnumerable<CompanyDataModel> CompanyDataModel { get; set; }
        public IEnumerable<ProductDataModel> ProductDataModel { get; set; }
        public IEnumerable<StockDataModel> StockDataModel { get; set; }
    }
}