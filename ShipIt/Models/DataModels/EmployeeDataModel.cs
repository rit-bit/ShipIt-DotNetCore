﻿using System;
using System.Data;
using ShipIt.Models.ApiModels;

namespace ShipIt.Models.DataModels
{
    public class EmployeeDataModel : DataModel
    {
        [DatabaseColumnName("name")]
        public string Name { get; set; }
        [DatabaseColumnName("w_id")]
        public int WarehouseId { get; set; }
        [DatabaseColumnName("role")]
        public string Role { get; set; }
        [DatabaseColumnName("ext")]
        public string Ext { get; set; }

        public EmployeeDataModel(IDataReader dataReader) : base(dataReader)
        { }

        public EmployeeDataModel()
        { }

        public EmployeeDataModel(Employee employee)
        {
            this.Name = employee.Name;
            this.WarehouseId = employee.WarehouseId;
            this.Role = MapApiRoleToDatabaseRole(employee.role);
            this.Ext = employee.ext;
        }

        private string MapApiRoleToDatabaseRole(EmployeeRole employeeRole)
        {
            switch (employeeRole) {
                
                case EmployeeRole.CLEANER:
                    return DataBaseRoles.Cleaner;

                case EmployeeRole.MANAGER:
                    return DataBaseRoles.Manager;

                case EmployeeRole.OPERATIONS_MANAGER:
                    return DataBaseRoles.OperationsManager;

                case EmployeeRole.PICKER:
                    return DataBaseRoles.Picker;

                default:
                    throw new ArgumentOutOfRangeException("EmployeeRole");
            }
        }
    }
}