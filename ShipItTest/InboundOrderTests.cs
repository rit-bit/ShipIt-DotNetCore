using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ShipIt.Controllers;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Models.DataModels;
using ShipIt.Repositories;
using ShipItTest.Builders;

namespace ShipItTest
{
    public class InboundOrderControllerTests : AbstractBaseTest
    {
        private readonly StockRepository _stockRepository = new ();
        private readonly CompanyRepository _companyRepository = new ();
        private readonly ProductRepository _productRepository = new ();
        private readonly EmployeeRepository _employeeRepository = new ();

        private readonly InboundOrderController _inboundOrderController = new (
            new EmployeeRepository(),
            new ProductRepository(),
            new StockRepository()
        );

        private static readonly Employee OpsManager = new EmployeeBuilder().CreateEmployee();
        private static readonly Company Company = new CompanyBuilder().CreateCompany();
        private static readonly int WarehouseId = OpsManager.WarehouseId;
        private static readonly string Gcp = Company.Gcp;

        private Product _product;
        private int _productId;
        private const string Gtin = "0000";

        public new void OnSetUp()
        {
            onSetUp();
            _employeeRepository.AddEmployees(new List<Employee>() { OpsManager });
            _companyRepository.AddCompanies(new List<Company>() { Company });
            var productDataModel = new ProductBuilder().SetGtin(Gtin).CreateProductDatabaseModel();
            _productRepository.AddProducts(new List<ProductDataModel>() { productDataModel });
            _product = new Product(_productRepository.GetProductByGtin(Gtin));
            _productId = _product.Id;
        }

        [Test]
        public void TestCreateOrderNoProductsHeld()
        {
            OnSetUp();

            var inboundOrder = _inboundOrderController.Get(WarehouseId);

            Assert.AreEqual(inboundOrder.WarehouseId, WarehouseId);
            Assert.IsTrue(EmployeesAreEqual(inboundOrder.OperationsManager, OpsManager));
            Assert.AreEqual(inboundOrder.OrderSegments.Count(), 0);
        }

        [Test]
        public void TestCreateOrderProductHoldingNoStock()
        {
            OnSetUp();
            _stockRepository.AddStock(WarehouseId, new List<StockAlteration>() { new StockAlteration(_productId, 0) });

            var inboundOrder = _inboundOrderController.Get(WarehouseId);

            Assert.AreEqual(inboundOrder.OrderSegments.Count(), 1);
            var orderSegment = inboundOrder.OrderSegments.First();
            Assert.AreEqual(orderSegment.Company.Gcp, Gcp);
        }

        [Test]
        public void TestCreateOrderProductHoldingSufficientStock()
        {
            OnSetUp();
            _stockRepository.AddStock(WarehouseId, new List<StockAlteration>() { new StockAlteration(_productId, _product.LowerThreshold) });

            var inboundOrder = _inboundOrderController.Get(WarehouseId);

            Assert.AreEqual(inboundOrder.OrderSegments.Count(), 0);
        }

        [Test]
        public void TestCreateOrderDiscontinuedProduct()
        {
            OnSetUp();
            _stockRepository.AddStock(WarehouseId, new List<StockAlteration>() { new StockAlteration(_productId, _product.LowerThreshold - 1) });
            _productRepository.DiscontinueProductByGtin(Gtin);

            var inboundOrder = _inboundOrderController.Get(WarehouseId);

            Assert.AreEqual(inboundOrder.OrderSegments.Count(), 0);
        }

        [Test]
        public void TestProcessManifest()
        {
            OnSetUp();
            const int quantity = 12;
            var inboundManifest = new InboundManifestRequestModel()
            {
                WarehouseId = WarehouseId,
                Gcp = Gcp,
                OrderLines = new List<OrderLine>()
                {
                    new ()
                    {
                        gtin = Gtin,
                        quantity = quantity
                    }
                }
            };

            _inboundOrderController.Post(inboundManifest);

            var stock = _stockRepository.GetStockByWarehouseAndProductIds(WarehouseId, new List<int>() {_productId})[_productId];
            Assert.AreEqual(stock.held, quantity);
        }

        [Test]
        public void TestProcessManifestRejectsDodgyGcp()
        {
            OnSetUp();
            const int quantity = 12;
            var dodgyGcp = Gcp + "XYZ";
            var inboundManifest = new InboundManifestRequestModel()
            {
                WarehouseId = WarehouseId,
                Gcp = dodgyGcp,
                OrderLines = new List<OrderLine>()
                {
                    new ()
                    {
                        gtin = Gtin,
                        quantity = quantity
                    }
                }
            };

            try
            {
                _inboundOrderController.Post(inboundManifest);
                Assert.Fail("Expected exception to be thrown.");
            }
            catch (ValidationException e)
            {
                Assert.IsTrue(e.Message.Contains(dodgyGcp));
            }
        }

        [Test]
        public void TestProcessManifestRejectsUnknownProduct()
        {
            OnSetUp();
            const int quantity = 12;
            const string unknownGtin = Gtin + "XYZ";
            var inboundManifest = new InboundManifestRequestModel()
            {
                WarehouseId = WarehouseId,
                Gcp = Gcp,
                OrderLines = new List<OrderLine>()
                {
                    new ()
                    {
                        gtin = Gtin,
                        quantity = quantity
                    },
                    new ()
                    {
                        gtin = unknownGtin,
                        quantity = quantity
                    }
                }
            };

            try
            {
                _inboundOrderController.Post(inboundManifest);
                Assert.Fail("Expected exception to be thrown.");
            }
            catch (ValidationException e)
            {
                Assert.IsTrue(e.Message.Contains(unknownGtin));
            }
        }

        [Test]
        public void TestProcessManifestRejectsDuplicateGtins()
        {
            OnSetUp();
            const int quantity = 12;
            var inboundManifest = new InboundManifestRequestModel()
            {
                WarehouseId = WarehouseId,
                Gcp = Gcp,
                OrderLines = new List<OrderLine>()
                {
                    new ()
                    {
                        gtin = Gtin,
                        quantity = quantity
                    },
                    new ()
                    {
                        gtin = Gtin,
                        quantity = quantity
                    }
                }
            };

            try
            {
                _inboundOrderController.Post(inboundManifest);
                Assert.Fail("Expected exception to be thrown.");
            }
            catch (ValidationException e)
            {
                Assert.IsTrue(e.Message.Contains(Gtin));
            }
        }

        private static bool EmployeesAreEqual(Employee a, Employee b)
        {
            return a.WarehouseId == b.WarehouseId
                   && a.Name == b.Name
                   && a.role == b.role
                   && a.ext == b.ext;
        }
    }
}
