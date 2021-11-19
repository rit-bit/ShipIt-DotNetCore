using System.Collections.Generic;
using ShipIt.Models.ApiModels;
using ShipIt.Models.DataModels;

namespace ShipItTest.Builders
{
    public class EmployeeBuilder
    {
        private string Name = "Gissell Sadeem";
        private int WarehouseId = 1;
        private EmployeeRole Role = EmployeeRole.OPERATIONS_MANAGER;
        private string Ext = "73996";

        public EmployeeBuilder SetName(string name)
        {
            Name = name;
            return this;
        }

        public EmployeeBuilder SetWarehouseId(int warehouseId)
        {
            WarehouseId = warehouseId;
            return this;
        }

        public EmployeeBuilder SetRole(EmployeeRole role)
        {
            Role = role;
            return this;
        }

        public EmployeeBuilder SetExt(string ext)
        {
            Ext = ext;
            return this;
        }

        public EmployeeDataModel CreateEmployeeDataModel()
        {
            return new EmployeeDataModel()
            {
                Name = Name,
                WarehouseId = WarehouseId,
                Role = Role.ToString(),
                Ext = Ext
            };
        }

        public Employee CreateEmployee()
        {
            return new Employee() {
                Name = Name,
                WarehouseId = WarehouseId,
                role = Role,
                ext = Ext
            };
        }

        public AddEmployeesRequest CreateAddEmployeesRequest()
        {
            return new AddEmployeesRequest()
            {
                Employees = new List<Employee>()
                {
                    new ()
                    {
                        Name = Name,
                        WarehouseId = WarehouseId,
                        role = Role,
                        ext = Ext
                    }
                }
            };
        }
    }
}
