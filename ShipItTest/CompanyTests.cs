using System.Collections.Generic;
using NUnit.Framework;
using ShipIt.Controllers;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Repositories;
using ShipItTest.Builders;

namespace ShipItTest
{
    public class CompanyControllerTests : AbstractBaseTest
    {
        private readonly CompanyController _companyController = new CompanyController(new CompanyRepository());
        private readonly CompanyRepository _companyRepository = new CompanyRepository();

        private const string Gcp = "0000346";

        [Test]
        public void TestRoundtripCompanyRepository()
        {
            onSetUp();
            var company = new CompanyBuilder().CreateCompany();
            _companyRepository.AddCompanies(new List<Company>() { company });
            Assert.AreEqual(_companyRepository.GetCompany(company.Gcp).Name, company.Name);
        }

        [Test]
        public void TestGetCompanyByGcp()
        {
            onSetUp();
            var companyBuilder = new CompanyBuilder().SetGcp(Gcp);
            _companyRepository.AddCompanies(new List<Company>() { companyBuilder.CreateCompany() });
            var result = _companyController.Get(Gcp);

            var correctCompany = companyBuilder.CreateCompany();
            Assert.IsTrue(CompaniesAreEqual(correctCompany, result.Company));
            Assert.IsTrue(result.Success);
        }

        [Test]
        public void TestGetNonExistentCompany()
        {
            onSetUp();
            try
            {
                _companyController.Get(Gcp);
                Assert.Fail("Expected exception to be thrown.");
            }
            catch (NoSuchEntityException e)
            {
                Assert.IsTrue(e.Message.Contains(Gcp));
            }
        }

        [Test]
        public void TestAddCompanies()
        {
            onSetUp();
            var companyBuilder = new CompanyBuilder().SetGcp(Gcp);
            var addCompaniesRequest = companyBuilder.CreateAddCompaniesRequest();

            var response = _companyController.Post(addCompaniesRequest);
            var databaseCompany = _companyRepository.GetCompany(Gcp);
            var correctCompany = companyBuilder.CreateCompany();

            Assert.IsTrue(response.Success);
            Assert.IsTrue(CompaniesAreEqual(new Company(databaseCompany), correctCompany));
        }

        private static bool CompaniesAreEqual(Company a, Company b)
        {
            return a.Gcp == b.Gcp
                   && a.Name == b.Name
                   && a.Addr2 == b.Addr2
                   && a.Addr3 == b.Addr3
                   && a.Addr4 == b.Addr4
                   && a.PostalCode == b.PostalCode
                   && a.City == b.City
                   && a.Tel == b.Tel
                   && a.Mail == b.Mail;
        }
    }
}
