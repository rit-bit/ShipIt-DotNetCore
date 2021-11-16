using System.Data;

namespace ShipIt.Models.DataModels
{
    public class EmployeeCountDataModel : DataModel
    {
        [DatabaseColumnName("count")]
        public long Count { get; set; }
        public EmployeeCountDataModel(IDataReader dataReader): base(dataReader) { }
        public EmployeeCountDataModel() {}
    }
}