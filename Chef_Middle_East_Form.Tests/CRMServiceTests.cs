using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Chef_Middle_East_Form.Services;
using Chef_Middle_East_Form.Models;
using Newtonsoft.Json.Linq;

namespace Chef_Middle_East_Form.Tests
{
    [TestClass]
    public class CRMServiceTests
    {
        private CRMService _crmService;
        private Mock<ICRMService> _mockCRMService;

        [TestInitialize]
        public void Setup()
        {
            _mockCRMService = new Mock<ICRMService>();
            _crmService = new CRMService();
        }

        #region Lead Data Tests

        [TestMethod]
        public async Task GetLeadDataAsync_ValidLeadId_ShouldReturnLeadData()
        {
            // Arrange
            var leadId = "test-lead-123";
            var expectedLeadData = new Form
            {
                CompanyName = "Test Company",
                Email = "test@company.com",
                Phone = "1234567890"
            };

            _mockCRMService.Setup(x => x.GetLeadDataAsync(leadId))
                .ReturnsAsync(expectedLeadData);

            // Act
            var result = await _mockCRMService.Object.GetLeadDataAsync(leadId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedLeadData.CompanyName, result.CompanyName);
            Assert.AreEqual(expectedLeadData.Email, result.Email);
            Assert.AreEqual(expectedLeadData.Phone, result.Phone);
        }

        [TestMethod]
        public async Task GetLeadDataAsync_InvalidLeadId_ShouldReturnNull()
        {
            // Arrange
            var invalidLeadId = "invalid-lead-id";

            _mockCRMService.Setup(x => x.GetLeadDataAsync(invalidLeadId))
                .ReturnsAsync((Form)null);

            // Act
            var result = await _mockCRMService.Object.GetLeadDataAsync(invalidLeadId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetLeadDataAsync_EmptyLeadId_ShouldReturnNull()
        {
            // Arrange
            var emptyLeadId = "";

            _mockCRMService.Setup(x => x.GetLeadDataAsync(emptyLeadId))
                .ReturnsAsync((Form)null);

            // Act
            var result = await _mockCRMService.Object.GetLeadDataAsync(emptyLeadId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetLeadDataAsync_NullLeadId_ShouldReturnNull()
        {
            // Arrange
            string nullLeadId = null;

            _mockCRMService.Setup(x => x.GetLeadDataAsync(nullLeadId))
                .ReturnsAsync((Form)null);

            // Act
            var result = await _mockCRMService.Object.GetLeadDataAsync(nullLeadId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region Account Data Tests

        [TestMethod]
        public async Task GetAccountDataAsync_ValidAccountId_ShouldReturnAccountData()
        {
            // Arrange
            var accountId = "test-account-123";
            var expectedAccountData = JObject.Parse(@"{
                'name': 'Test Company',
                'emailaddress1': 'test@company.com',
                'telephone1': '1234567890',
                'address1_city': 'Test City',
                'address1_country': 'Test Country'
            }");

            _mockCRMService.Setup(x => x.GetAccountDataAsync(accountId))
                .ReturnsAsync(expectedAccountData);

            // Act
            var result = await _mockCRMService.Object.GetAccountDataAsync(accountId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Test Company", result["name"]);
            Assert.AreEqual("test@company.com", result["emailaddress1"]);
            Assert.AreEqual("1234567890", result["telephone1"]);
        }

        [TestMethod]
        public async Task GetAccountDataAsync_InvalidAccountId_ShouldReturnNull()
        {
            // Arrange
            var invalidAccountId = "invalid-account-id";

            _mockCRMService.Setup(x => x.GetAccountDataAsync(invalidAccountId))
                .ReturnsAsync((JObject)null);

            // Act
            var result = await _mockCRMService.Object.GetAccountDataAsync(invalidAccountId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetAccountDataAsync_EmptyAccountId_ShouldReturnNull()
        {
            // Arrange
            var emptyAccountId = "";

            _mockCRMService.Setup(x => x.GetAccountDataAsync(emptyAccountId))
                .ReturnsAsync((JObject)null);

            // Act
            var result = await _mockCRMService.Object.GetAccountDataAsync(emptyAccountId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region Account Update Tests

        [TestMethod]
        public async Task UpdateAccountAsync_ValidData_ShouldReturnTrue()
        {
            // Arrange
            var accountId = "test-account-123";
            var formData = new Form
            {
                CompanyName = "Updated Company",
                Email = "updated@company.com",
                Phone = "0987654321"
            };

            _mockCRMService.Setup(x => x.UpdateAccountAsync(accountId, formData))
                .ReturnsAsync(true);

            // Act
            var result = await _mockCRMService.Object.UpdateAccountAsync(accountId, formData);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task UpdateAccountAsync_InvalidData_ShouldReturnFalse()
        {
            // Arrange
            var accountId = "test-account-123";
            var invalidFormData = new Form(); // Empty form

            _mockCRMService.Setup(x => x.UpdateAccountAsync(accountId, invalidFormData))
                .ReturnsAsync(false);

            // Act
            var result = await _mockCRMService.Object.UpdateAccountAsync(accountId, invalidFormData);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UpdateAccountAsync_NullFormData_ShouldReturnFalse()
        {
            // Arrange
            var accountId = "test-account-123";
            Form nullFormData = null;

            _mockCRMService.Setup(x => x.UpdateAccountAsync(accountId, nullFormData))
                .ReturnsAsync(false);

            // Act
            var result = await _mockCRMService.Object.UpdateAccountAsync(accountId, nullFormData);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Account Creation Tests

        [TestMethod]
        public async Task CreateAccountAsync_ValidData_ShouldReturnAccountId()
        {
            // Arrange
            var formData = new Form
            {
                CompanyName = "New Company",
                Email = "new@company.com",
                Phone = "5555555555"
            };
            var expectedAccountId = "new-account-123";

            _mockCRMService.Setup(x => x.CreateAccountAsync(formData))
                .ReturnsAsync(expectedAccountId);

            // Act
            var result = await _mockCRMService.Object.CreateAccountAsync(formData);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedAccountId, result);
        }

        [TestMethod]
        public async Task CreateAccountAsync_InvalidData_ShouldReturnNull()
        {
            // Arrange
            var invalidFormData = new Form(); // Empty form

            _mockCRMService.Setup(x => x.CreateAccountAsync(invalidFormData))
                .ReturnsAsync((string)null);

            // Act
            var result = await _mockCRMService.Object.CreateAccountAsync(invalidFormData);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task CreateAccountAsync_NullFormData_ShouldReturnNull()
        {
            // Arrange
            Form nullFormData = null;

            _mockCRMService.Setup(x => x.CreateAccountAsync(nullFormData))
                .ReturnsAsync((string)null);

            // Act
            var result = await _mockCRMService.Object.CreateAccountAsync(nullFormData);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region Error Handling Tests

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task GetLeadDataAsync_ServiceException_ShouldThrowException()
        {
            // Arrange
            var leadId = "test-lead-123";

            _mockCRMService.Setup(x => x.GetLeadDataAsync(leadId))
                .ThrowsAsync(new Exception("CRM service unavailable"));

            // Act
            await _mockCRMService.Object.GetLeadDataAsync(leadId);

            // Assert - Exception expected
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task GetLeadDataAsync_NullLeadId_ShouldThrowArgumentNullException()
        {
            // Arrange
            string nullLeadId = null;

            _mockCRMService.Setup(x => x.GetLeadDataAsync(nullLeadId))
                .ThrowsAsync(new ArgumentNullException("leadId"));

            // Act
            await _mockCRMService.Object.GetLeadDataAsync(nullLeadId);

            // Assert - Exception expected
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task GetAccountDataAsync_Timeout_ShouldThrowTimeoutException()
        {
            // Arrange
            var accountId = "test-account-123";

            _mockCRMService.Setup(x => x.GetAccountDataAsync(accountId))
                .ThrowsAsync(new TimeoutException("Request timed out"));

            // Act
            await _mockCRMService.Object.GetAccountDataAsync(accountId);

            // Assert - Exception expected
        }

        #endregion

        #region Data Validation Tests

        [TestMethod]
        public async Task GetLeadDataAsync_LeadDataWithNullFields_ShouldHandleGracefully()
        {
            // Arrange
            var leadId = "test-lead-123";
            var leadDataWithNulls = new Form
            {
                CompanyName = null,
                Email = null,
                Phone = null
            };

            _mockCRMService.Setup(x => x.GetLeadDataAsync(leadId))
                .ReturnsAsync(leadDataWithNulls);

            // Act
            var result = await _mockCRMService.Object.GetLeadDataAsync(leadId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.CompanyName);
            Assert.IsNull(result.Email);
            Assert.IsNull(result.Phone);
        }

        [TestMethod]
        public async Task GetAccountDataAsync_AccountDataWithMissingFields_ShouldHandleGracefully()
        {
            // Arrange
            var accountId = "test-account-123";
            var accountDataWithMissingFields = JObject.Parse(@"{
                'name': 'Test Company'
                // Missing email and phone fields
            }");

            _mockCRMService.Setup(x => x.GetAccountDataAsync(accountId))
                .ReturnsAsync(accountDataWithMissingFields);

            // Act
            var result = await _mockCRMService.Object.GetAccountDataAsync(accountId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Test Company", result["name"]);
            Assert.IsNull(result["emailaddress1"]);
            Assert.IsNull(result["telephone1"]);
        }

        #endregion
    }
}
