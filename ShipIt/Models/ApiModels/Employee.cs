﻿using System;
using System.Text;
using ShipIt.Models.DataModels;

namespace ShipIt.Models.ApiModels
{
    public class Employee
    {
        public string Name { get; set; }
        public int WarehouseId { get; set; }
        public EmployeeRole role { get; set; }
        public string ext { get; set; }

        public Employee(EmployeeDataModel dataModel)
        {
            Name = dataModel.Name;
            WarehouseId = dataModel.WarehouseId;
            role = MapDatabaseRoleToApiRole(dataModel.Role);
            ext = dataModel.Ext;
        }

        private EmployeeRole MapDatabaseRoleToApiRole(string databaseRole)
        {
            switch (databaseRole) {
                
                case DataBaseRoles.Cleaner:
                    return EmployeeRole.CLEANER;

                case DataBaseRoles.Manager:
                    return EmployeeRole.MANAGER;

                case DataBaseRoles.OperationsManager:
                    return EmployeeRole.OPERATIONS_MANAGER;

                case DataBaseRoles.Picker:
                    return EmployeeRole.PICKER;

                default:
                    throw new ArgumentOutOfRangeException("DatabaseRole");
            }
        }

        //Empty constructor needed for Xml serialization
        public Employee()
        {
        }

        public override string ToString()
        {
            return new StringBuilder()
                    .AppendFormat("name: {0}, ", Name)
                    .AppendFormat("warehouseId: {0}, ", WarehouseId)
                    .AppendFormat("role: {0}, ", role)
                    .AppendFormat("ext: {0}", ext)
                    .ToString();
        }
    }
}