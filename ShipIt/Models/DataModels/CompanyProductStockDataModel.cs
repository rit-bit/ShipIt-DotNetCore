using System.Data;

namespace ShipIt.Models.DataModels
{
    public class CompanyProductStockDataModel : DataModel
    {
        
        public CompanyDataModel CompanyDataModel { get; set; }
        public ProductDataModel ProductDataModel { get; set; }
        public StockDataModel StockDataModel { get; set; }
        


        public CompanyProductStockDataModel(IDataReader dataReader)
        {
            CompanyDataModel = new CompanyDataModel(dataReader);
            ProductDataModel = new ProductDataModel(dataReader);
            StockDataModel = new StockDataModel(dataReader);
        }
        public CompanyProductStockDataModel() {}
    }
}