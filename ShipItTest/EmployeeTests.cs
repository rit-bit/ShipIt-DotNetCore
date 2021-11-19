﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ShipIt.Controllers;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Repositories;
using ShipItTest.Builders;

namespace ShipItTest
{
    public class EmployeeControllerTests : AbstractBaseTest
    {
        EmployeeController employeeController = new EmployeeController(new EmployeeRepository());
        EmployeeRepository employeeRepository = new EmployeeRepository();

        private const string NAME = "Gissell Sadeem";
        private const int WAREHOUSE_ID = 1;

        [Test]
        public void TestRoundtripEmployeeRepository()
        {
            onSetUp();
            var employee = new EmployeeBuilder().CreateEmployee();
            employeeRepository.AddEmployees(new List<Employee>() {employee});
            var employeeFromRepo = employeeRepository.GetEmployeesByName(employee.Name).First();
            Assert.AreEqual(employeeFromRepo.Name, employee.Name);
            Assert.AreEqual(employeeFromRepo.Ext, employee.ext);
            Assert.AreEqual(employeeFromRepo.WarehouseId, employee.WarehouseId);
        }

        [Test]
        public void TestGetEmployeeByName()
        {
            onSetUp();
            var employeeBuilder = new EmployeeBuilder().SetName(NAME);
            employeeRepository.AddEmployees(new List<Employee>() {employeeBuilder.CreateEmployee()});
            var result = employeeController.Get(NAME);

            var correctEmployee = employeeBuilder.CreateEmployee();
            Assert.IsTrue(EmployeesAreEqual(correctEmployee, result.Employees.First()));
            Assert.IsTrue(result.Success);
        }

        [Test]
        public void TestGetEmployeesByWarehouseId()
        {
            onSetUp();
            var employeeBuilderA = new EmployeeBuilder().SetWarehouseId(WAREHOUSE_ID).SetName("A");
            var employeeBuilderB = new EmployeeBuilder().SetWarehouseId(WAREHOUSE_ID).SetName("B");
            employeeRepository.AddEmployees(new List<Employee>() { employeeBuilderA.CreateEmployee(), employeeBuilderB.CreateEmployee() });
            var result = employeeController.Get(WAREHOUSE_ID).Employees.ToList();

            var correctEmployeeA = employeeBuilderA.CreateEmployee();
            var correctEmployeeB = employeeBuilderB.CreateEmployee();

            Assert.IsTrue(result.Count == 2);
            Assert.IsTrue(EmployeesAreEqual(correctEmployeeA, result.First()));
            Assert.IsTrue(EmployeesAreEqual(correctEmployeeB, result.Last()));
        }

        [Test]
        public void TestGetNonExistentEmployee()
        {
            onSetUp();
            try
            {
                var result = employeeController.Get(NAME).Employees.ToList();
                Assert.Fail("Expected exception to be thrown.");
            }
            catch (NoSuchEntityException e)
            {
                Assert.IsTrue(e.Message.Contains(NAME));
            }
        }

        [Test]
        public void TestGetEmployeeInNonexistentWarehouse()
        {
            onSetUp();
            try
            {
                var employees = employeeController.Get(WAREHOUSE_ID).Employees.ToList();
                Assert.Fail("Expected exception to be thrown.");
            }
            catch (NoSuchEntityException e)
            {
                Assert.IsTrue(e.Message.Contains(WAREHOUSE_ID.ToString()));
            }
        }

        [Test]
        public void TestAddEmployees()
        {
            onSetUp();
            var employeeBuilder = new EmployeeBuilder().SetName(NAME);
            var addEmployeesRequest = employeeBuilder.CreateAddEmployeesRequest();

            var response = employeeController.Post(addEmployeesRequest);
            var databaseEmployee = employeeRepository.GetEmployeesByName(NAME).First();
            var correctDatabaseEmployee = employeeBuilder.CreateEmployee();

            Assert.IsTrue(response.Success);
            Assert.IsTrue(EmployeesAreEqual(new Employee(databaseEmployee), correctDatabaseEmployee));
        }

        [Test]
        public void TestDeleteEmployees()
        {
            onSetUp();
            var employeeBuilder = new EmployeeBuilder().SetName(NAME);
            employeeRepository.AddEmployees(new List<Employee>() { employeeBuilder.CreateEmployee() });

            var removeEmployeeRequest = new RemoveEmployeeRequest() { Name = NAME };
            employeeController.Delete(removeEmployeeRequest);

            try
            {
                employeeController.Get(NAME).Employees.ToList();
                Assert.Fail("Expected exception to be thrown.");
            }
            catch (NoSuchEntityException e)
            {
                Assert.IsTrue(e.Message.Contains(NAME));
            }
        }

        [Test]
        public void TestDeleteNonexistentEmployee()
        {
            onSetUp();
            var removeEmployeeRequest = new RemoveEmployeeRequest() { Name = NAME };

            try
            {
                employeeController.Delete(removeEmployeeRequest);
                Assert.Fail("Expected exception to be thrown.");
            }
            catch (NoSuchEntityException e)
            {
                Assert.IsTrue(e.Message.Contains(NAME));
            }
        }

        [Test]
        public void TestAddDuplicateEmployee()
        {
            onSetUp();
            var employeeBuilder = new EmployeeBuilder().SetName(NAME);
            employeeRepository.AddEmployees(new List<Employee>() { employeeBuilder.CreateEmployee() });
            var addEmployeesRequest = employeeBuilder.CreateAddEmployeesRequest();

            try
            {
                employeeController.Post(addEmployeesRequest);
                var deleteRequest = new RemoveEmployeeRequest {Name = NAME};
                employeeController.Delete(deleteRequest);
                Assert.Fail("Expected exception to be thrown because there are two employees with that name.");
            }
            catch (ArgumentException e)
            {
                Assert.IsTrue(e.Message.Contains(NAME));
            }
        }

        private bool EmployeesAreEqual(Employee A, Employee B)
        {
            return A.WarehouseId == B.WarehouseId
                   && A.Name == B.Name
                   && A.role == B.role
                   && A.ext == B.ext;
        }
    }
}
