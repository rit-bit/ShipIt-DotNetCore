﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Repositories;

namespace ShipIt.Controllers
{

    [Route("employees")]
    public class EmployeeController : ControllerBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeController(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        [HttpGet("")]
        public EmployeeResponse Get([FromQuery] string name)
        {
            Log.Info($"Looking up employee by name: {name}");

            var employees = _employeeRepository
                .GetEmployeesByName(name)
                .Select(e => new Employee(e));
            // TODO Should this throw an exception if no employees are found with this name?
            Log.Info("Found employees: " + employees);
            return new EmployeeResponse(employees);
        }

        [HttpGet("{warehouseId}")]
        public EmployeeResponse Get([FromRoute] int warehouseId)
        {
            Log.Info($"Looking up employee by id: {warehouseId}");

            var employees = _employeeRepository
                .GetEmployeesByWarehouseId(warehouseId)
                .Select(e => new Employee(e));

            Log.Info($"Found employees: {employees}");
            
            return new EmployeeResponse(employees);
        }

        [HttpPost("")]
        public Response Post([FromBody] AddEmployeesRequest requestModel)
        {
            List<Employee> employeesToAdd = requestModel.Employees;

            if (employeesToAdd.Count == 0)
            {
                throw new MalformedRequestException("Expected at least one <employee> tag");
            }

            Log.Info($"Adding employees: {employeesToAdd}");

            _employeeRepository.AddEmployees(employeesToAdd);

            Log.Debug("Employees added successfully");

            return new Response() { Success = true };
        }

        [HttpDelete("")]
        public void Delete([FromBody] RemoveEmployeeRequest requestModel)
        {
            string name = requestModel.Name;
            if (name == null)
            {
                throw new MalformedRequestException("Unable to parse name from request parameters");
            }
            var numberOfEmployeesWithName = _employeeRepository.CountEmployees(name).Count;
            if (numberOfEmployeesWithName > 1) {
                throw new ArgumentException($"There are {numberOfEmployeesWithName} employees with the name {name}. " 
                    + "Please find the one you wish to delete and use the \"Delete by ID\" feature instead.");
            }

            try
            {
                _employeeRepository.RemoveEmployee(name);
            }
            catch (NoSuchEntityException)
            {
                throw new NoSuchEntityException($"No employee exists with name: {name}");
            }
        }

        [HttpDelete("id")]
        public void DeleteById([FromBody] RemoveEmployeeByIdRequest requestModel)
        {
            int id = requestModel.Id;
            if (id == 0) {
                throw new MalformedRequestException("Unable to parse id from request parameters");
            }

            try
            {
                _employeeRepository.RemoveEmployeeById(id);
            }
            catch (NoSuchEntityException)
            {
                throw new NoSuchEntityException($"No employee exists with id: {id}");
            }
        }
    }
}
