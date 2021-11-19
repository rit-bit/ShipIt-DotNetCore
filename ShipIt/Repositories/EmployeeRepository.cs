using System.Collections.Generic;
using System.Data;
using System.Linq;
using Npgsql;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Models.DataModels;

namespace ShipIt.Repositories
{
    public interface IEmployeeRepository
    {
        int GetCount();
        int GetWarehouseCount();
        IEnumerable<EmployeeDataModel> GetEmployeesByName(string name);
        IEnumerable<EmployeeDataModel> GetEmployeesByWarehouseId(int warehouseId);
        EmployeeDataModel GetOperationsManager(int warehouseId);
        void AddEmployees(IEnumerable<Employee> employees);
        EmployeeCountDataModel CountEmployees(string name);
        void RemoveEmployee(string name);
        public void RemoveEmployeeById(int id);
    }

    public class EmployeeRepository : RepositoryBase, IEmployeeRepository
    {
        public static IDbConnection CreateSqlConnection()
        {
            return new NpgsqlConnection(ConnectionHelper.GetConnectionString());
        }

        public int GetCount()
        {
            using var connection = CreateSqlConnection();
            var command = connection.CreateCommand();
            const string employeeCountSql = "SELECT COUNT(*) FROM em";
            command.CommandText = employeeCountSql;
            connection.Open();
            var reader = command.ExecuteReader();

            try
            {
                reader.Read();
                return (int) reader.GetInt64(0);
            }
            finally
            {
                reader.Close();
            }
        }

        public int GetWarehouseCount()
        {
            using var connection = CreateSqlConnection();
            var command = connection.CreateCommand();
            const string employeeCountSql = "SELECT COUNT(DISTINCT w_id) FROM em";
            command.CommandText = employeeCountSql;
            connection.Open();
            var reader = command.ExecuteReader();

            try
            {
                reader.Read();
                return (int)reader.GetInt64(0);
            }
            finally
            {
                reader.Close();
            }
        }

        public IEnumerable<EmployeeDataModel> GetEmployeesByName(string name)
        {
            const string sql = "SELECT name, w_id, role, ext FROM em WHERE name = @name";
            var parameter = new NpgsqlParameter("@name", name);
            var noEmployeeWithNameErrorMessage = $"No employees found with name: {name}";
            return RunGetQuery(sql, reader => new EmployeeDataModel(reader),noEmployeeWithNameErrorMessage, parameter);
        }

        public IEnumerable<EmployeeDataModel> GetEmployeesByWarehouseId(int warehouseId)
        {

            const string sql = "SELECT name, w_id, role, ext FROM em WHERE w_id = @w_id";
            var parameter = new NpgsqlParameter("@w_id", warehouseId);
            var noEmployeeAtWarehouseErrorMessage = $"No employees found with Warehouse Id: {warehouseId}";
            return RunGetQuery(sql, reader => new EmployeeDataModel(reader), noEmployeeAtWarehouseErrorMessage, parameter);
        }

        public EmployeeDataModel GetOperationsManager(int warehouseId)
        {

            const string sql = "SELECT name, w_id, role, ext FROM em WHERE w_id = @w_id AND role = @role";
            var parameters = new []
            {
                new NpgsqlParameter("@w_id", warehouseId),
                new NpgsqlParameter("@role", DataBaseRoles.OperationsManager)
            };

            var noOperationsManagerAtWarehouseErrorMessage = $"No operations manager found with Warehouse Id: {warehouseId}";
            return RunSingleGetQuery(sql, reader => new EmployeeDataModel(reader), noOperationsManagerAtWarehouseErrorMessage, parameters);
        }

        public void AddEmployees(IEnumerable<Employee> employees)
        {
            const string sql = "INSERT INTO em (name, w_id, role, ext) VALUES(@name, @w_id, @role, @ext)";
            
            var parametersList = new List<NpgsqlParameter[]>();
            foreach (var employee in employees)
            {
                var employeeDataModel = new EmployeeDataModel(employee);
                parametersList.Add(employeeDataModel.GetNpgsqlParameters().ToArray());
            }

            RunTransaction(sql, parametersList);
        }

        public EmployeeCountDataModel CountEmployees(string name)
        {
            const string sql = "SELECT COUNT(*) FROM em WHERE name = @name";
            var parameter = new NpgsqlParameter("@name", name);
            return RunSingleGetQuery(sql, reader => new EmployeeCountDataModel(reader), $"No employees found with name {name}", parameter);
        }

        public void RemoveEmployee(string name)
        {
            const string sql = "DELETE FROM em WHERE name = @name";
            var parameter = new NpgsqlParameter("@name", name);
            var rowsDeleted = RunSingleQueryAndReturnRecordsAffected(sql, parameter);
            switch (rowsDeleted)
            {
                case 0:
                    throw new NoSuchEntityException("Incorrect result size: expected 1, actual 0");
                case > 1:
                    throw new InvalidStateException($"Unexpectedly deleted {rowsDeleted} rows, but expected a single update");
            }
        }

        public void RemoveEmployeeById(int id)
        {
            const string sql = "DELETE FROM em WHERE employee_id = @id";
            var parameter = new NpgsqlParameter("@id", id);
            var rowsDeleted = RunSingleQueryAndReturnRecordsAffected(sql, parameter);
            if (rowsDeleted == 0)
            {
                throw new NoSuchEntityException("Incorrect result size: expected 1, actual 0");
            }
        }
    }
}