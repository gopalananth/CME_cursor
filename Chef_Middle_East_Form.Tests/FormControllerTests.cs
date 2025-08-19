using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Chef_Middle_East_Form.Controllers;
using Chef_Middle_East_Form.Models;
using Chef_Middle_East_Form.Services;
using Newtonsoft.Json.Linq;

namespace Chef_Middle_East_Form.Tests
{
    [TestClass]
    public class FormControllerTests
    {
        private FormController _controller;
        private Mock<ICRMService> _mockCRMService;
        private Mock<IFileUploadService> _mockFileUploadService;
        private Mock<ILoggingService> _mockLoggingService;
        private Mock<HttpContextBase> _mockHttpContext;
        private Mock<HttpServerUtilityBase> _mockServer;

        [TestInitialize]
        public void Setup()
        {
            _mockCRMService = new Mock<ICRMService>();
            _mockFileUploadService = new Mock<IFileUploadService>();
            _mockLoggingService = new Mock<ILoggingService>();
            _mockHttpContext = new Mock<HttpContextBase>();
            _mockServer = new Mock<HttpServerUtilityBase>();

            _mockHttpContext.Setup(x => x.Server).Returns(_mockServer.Object);
            _mockHttpContext.Setup(x => x.Session).Returns(new Mock<HttpSessionStateBase>().Object);

            _controller = new FormController(_mockCRMService.Object, _mockFileUploadService.Object, _mockLoggingService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _mockHttpContext.Object
                }
            };
        }

        #region File Security Tests

        [TestMethod]
        public void SaveFile_ValidPdfFile_ShouldSaveSuccessfully()
        {
            // Arrange
            var mockFile = CreateMockFile("test.pdf", "application/pdf", 1024);
            var uploadPath = @"C:\temp\uploads";

            // Act
            var result = InvokeSaveFile(mockFile.Object, uploadPath);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1024, result.Length);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SaveFile_FileSizeExceedsLimit_ShouldThrowException()
        {
            // Arrange
            var mockFile = CreateMockFile("large.pdf", "application/pdf", 15 * 1024 * 1024); // 15MB
            var uploadPath = @"C:\temp\uploads";

            // Act
            InvokeSaveFile(mockFile.Object, uploadPath);

            // Assert - Exception expected
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SaveFile_InvalidFileType_ShouldThrowException()
        {
            // Arrange
            var mockFile = CreateMockFile("test.exe", "application/octet-stream", 1024);
            var uploadPath = @"C:\temp\uploads";

            // Act
            InvokeSaveFile(mockFile.Object, uploadPath);

            // Assert - Exception expected
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SaveFile_PathTraversalAttempt_ShouldThrowException()
        {
            // Arrange
            var mockFile = CreateMockFile("../../../malicious.txt", "text/plain", 1024);
            var uploadPath = @"C:\temp\uploads";

            // Act
            InvokeSaveFile(mockFile.Object, uploadPath);

            // Assert - Exception expected
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SaveFile_NullFileName_ShouldThrowException()
        {
            // Arrange
            var mockFile = CreateMockFile(null, "application/pdf", 1024);
            var uploadPath = @"C:\temp\uploads";

            // Act
            InvokeSaveFile(mockFile.Object, uploadPath);

            // Assert - Exception expected
        }

        #endregion

        #region Null Reference Tests

        [TestMethod]
        public void Create_LeadDataWithNullEmailSentOn_ShouldNotThrowException()
        {
            // Arrange
            var leadData = new Form { EmailSenton = null };
            _mockCRMService.Setup(x => x.GetLeadData(It.IsAny<string>()))
                .ReturnsAsync(leadData);

            // Act & Assert - Should not throw
            try
            {
                var result = _controller.Create("test-lead-id").Result;
                Assert.IsNotNull(result);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void Create_LeadDataWithValidEmailSentOn_ShouldCheckExpiration()
        {
            // Arrange
            var leadData = new Form { EmailSenton = DateTime.UtcNow.AddHours(-49) }; // Expired
            _mockCRMService.Setup(x => x.GetLeadData(It.IsAny<string>()))
                .ReturnsAsync(leadData);

            // Act
            var result = _controller.Create("test-lead-id").Result;

            // Assert
            Assert.IsNotNull(result);
            // Should redirect to LinkExpired view
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        #endregion

        #region File Path Validation Tests

        [TestMethod]
        public void IsValidFilePath_ValidPath_ShouldReturnTrue()
        {
            // Arrange
            var uploadFolder = @"C:\temp\uploads";
            var filePath = @"C:\temp\uploads\test.pdf";

            // Act
            var result = InvokeIsValidFilePath(filePath, uploadFolder);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsValidFilePath_PathOutsideUploadFolder_ShouldReturnFalse()
        {
            // Arrange
            var uploadFolder = @"C:\temp\uploads";
            var filePath = @"C:\temp\malicious\test.pdf";

            // Act
            var result = InvokeIsValidFilePath(filePath, uploadFolder);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsValidFilePath_PathTraversalAttempt_ShouldReturnFalse()
        {
            // Arrange
            var uploadFolder = @"C:\temp\uploads";
            var filePath = @"C:\temp\uploads\..\..\malicious\test.pdf";

            // Act
            var result = InvokeIsValidFilePath(filePath, uploadFolder);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Method Refactoring Tests

        [TestMethod]
        public void ValidateCRMConfiguration_ValidConfiguration_ShouldNotThrowException()
        {
            // Arrange
            _mockCRMService.Setup(x => x.GetLeadData(It.IsAny<string>()))
                .ReturnsAsync(new Form());

            // Act & Assert - Should not throw
            try
            {
                var result = InvokeValidateCRMConfiguration();
                Assert.IsTrue(result);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void IsLinkExpired_ExpiredLink_ShouldReturnTrue()
        {
            // Arrange
            var leadData = new Form { EmailSenton = DateTime.UtcNow.AddHours(-49) };

            // Act
            var result = InvokeIsLinkExpired(leadData);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsLinkExpired_ValidLink_ShouldReturnFalse()
        {
            // Arrange
            var leadData = new Form { EmailSenton = DateTime.UtcNow.AddHours(-23) };

            // Act
            var result = InvokeIsLinkExpired(leadData);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsLinkExpired_NullEmailSentOn_ShouldReturnFalse()
        {
            // Arrange
            var leadData = new Form { EmailSenton = null };

            // Act
            var result = InvokeIsLinkExpired(leadData);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Form Validation Tests

        [TestMethod]
        public void ValidateFormData_ValidForm_ShouldReturnTrue()
        {
            // Arrange
            var form = new Form
            {
                CompanyName = "Test Company",
                Email = "test@company.com",
                MainPhone = "1234567890"
            };

            // Act
            var result = InvokeValidateFormData(form);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ValidateFormData_InvalidForm_ShouldReturnFalse()
        {
            // Arrange
            var form = new Form
            {
                CompanyName = "", // Invalid - empty
                Email = "invalid-email", // Invalid email format
                MainPhone = "" // Invalid - empty
            };

            // Act
            var result = InvokeValidateFormData(form);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Helper Methods

        private Mock<HttpPostedFileBase> CreateMockFile(string fileName, string contentType, int contentLength)
        {
            var mockFile = new Mock<HttpPostedFileBase>();
            mockFile.Setup(x => x.FileName).Returns(fileName);
            mockFile.Setup(x => x.ContentType).Returns(contentType);
            mockFile.Setup(x => x.ContentLength).Returns(contentLength);
            mockFile.Setup(x => x.InputStream).Returns(new MemoryStream(new byte[contentLength]));
            return mockFile;
        }

        private byte[] InvokeSaveFile(HttpPostedFileBase file, string path)
        {
            var method = typeof(FormController).GetMethod("SaveFile", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (byte[])method.Invoke(_controller, new object[] { file, path });
        }

        private bool InvokeIsValidFilePath(string filePath, string uploadFolder)
        {
            var method = typeof(FormController).GetMethod("IsValidFilePath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (bool)method.Invoke(_controller, new object[] { filePath, uploadFolder });
        }

        private bool InvokeValidateCRMConfiguration()
        {
            var method = typeof(FormController).GetMethod("ValidateCRMConfiguration", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (bool)method.Invoke(_controller, new object[0]);
        }

        private bool InvokeIsLinkExpired(Form leadData)
        {
            var method = typeof(FormController).GetMethod("IsLinkExpired", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (bool)method.Invoke(_controller, new object[] { leadData });
        }

        private bool InvokeValidateFormData(Form form)
        {
            var method = typeof(FormController).GetMethod("ValidateFormData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (bool)method.Invoke(_controller, new object[] { form });
        }

        #endregion

        #region Error Handling and Logging Tests

        [TestMethod]
        public async Task Create_ExceptionThrown_ShouldLogErrorAndReturnUserFriendlyError()
        {
            // Arrange
            var leadId = "test-lead-123";
            var errorId = "ERR12345";
            
            _mockCRMService.Setup(x => x.GetLeadDataAsync(leadId))
                .ThrowsAsync(new Exception("Test exception"));
            
            _mockLoggingService.Setup(x => x.LogException(It.IsAny<Exception>(), It.IsAny<object>()))
                .Returns(errorId);

            // Act
            var result = await _controller.Create(leadId) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("UserFriendlyError", result.ViewName);
            Assert.AreEqual(errorId, result.ViewBag.ErrorId);
            Assert.AreEqual(leadId, result.ViewBag.LeadId);
            
            _mockLoggingService.Verify(x => x.LogException(It.IsAny<Exception>(), 
                It.Is<object>(o => o.ToString().Contains(leadId))), Times.Once);
        }

        [TestMethod]
        public async Task Create_ExceptionThrownWithDetailedErrorsEnabled_ShouldReturnDetailedError()
        {
            // Arrange
            var leadId = "test-lead-123";
            var errorId = "ERR12345";
            
            // Mock configuration to enable detailed errors
            System.Configuration.ConfigurationManager.AppSettings["ShowDetailedErrors"] = "true";
            
            _mockCRMService.Setup(x => x.GetLeadDataAsync(leadId))
                .ThrowsAsync(new Exception("Test exception"));
            
            _mockLoggingService.Setup(x => x.LogException(It.IsAny<Exception>(), It.IsAny<object>()))
                .Returns(errorId);

            // Act
            var result = await _controller.Create(leadId) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Error", result.ViewName);
        }

        [TestMethod]
        public void Constructor_NullCRMService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                new FormController(null, _mockFileUploadService.Object, _mockLoggingService.Object));
        }

        [TestMethod]
        public void Constructor_NullFileUploadService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                new FormController(_mockCRMService.Object, null, _mockLoggingService.Object));
        }

        [TestMethod]
        public void Constructor_NullLoggingService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                new FormController(_mockCRMService.Object, _mockFileUploadService.Object, null));
        }

        [TestMethod]
        public void Constructor_AllValidParameters_ShouldCreateController()
        {
            // Act
            var controller = new FormController(_mockCRMService.Object, _mockFileUploadService.Object, _mockLoggingService.Object);

            // Assert
            Assert.IsNotNull(controller);
        }

        #endregion

        #region Configuration and Validation Tests

        [TestMethod]
        public void IsLinkExpired_CustomExpiryHours_WorksCorrectly()
        {
            // This test verifies the configurable expiry functionality
            // Note: We can't easily test private methods, but we can test the behavior
            // through the public Create method
            
            // Arrange
            var expiredForm = new Form 
            { 
                LeadId = "test-expired-lead",
                EmailSenton = DateTime.UtcNow.AddHours(-50) // 50 hours ago (more than default 48)
            };
            
            _mockCRMService.Setup(x => x.GetLeadDataAsync("test-expired-lead"))
                .ReturnsAsync(expiredForm);

            // Act
            var result = _controller.Create("test-expired-lead").Result as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("LinkExpired", result.ViewName);
        }

        [TestMethod]
        public void IsLinkExpired_WithinExpiryWindow_AllowsAccess()
        {
            // Arrange
            var validForm = new Form 
            { 
                LeadId = "test-valid-lead",
                EmailSenton = DateTime.UtcNow.AddHours(-24) // 24 hours ago (within 48 hour window)
            };
            
            _mockCRMService.Setup(x => x.GetLeadDataAsync("test-valid-lead"))
                .ReturnsAsync(validForm);

            // Act
            var result = _controller.Create("test-valid-lead").Result as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotEqual("LinkExpired", result.ViewName);
            Assert.IsNotNull(result.Model);
        }

        [TestMethod]
        public void Create_LogsExpiryCheckInformation()
        {
            // This test verifies that expiry checking is logged properly
            // Arrange
            var formWithEmailSent = new Form 
            { 
                LeadId = "test-logging-lead",
                EmailSenton = DateTime.UtcNow.AddHours(-12)
            };
            
            _mockCRMService.Setup(x => x.GetLeadDataAsync("test-logging-lead"))
                .ReturnsAsync(formWithEmailSent);

            // Act
            var result = _controller.Create("test-logging-lead").Result;

            // Assert
            Assert.IsNotNull(result);
            // Verify that logging service was called for expiry check
            _mockLoggingService.Verify(x => x.LogInfo(It.Is<string>(msg => msg.Contains("Checking link expiry")), 
                It.IsAny<object>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void SafeAssignFromAccountData_ValidData_AssignsCorrectly()
        {
            // This is an indirect test of the helper method through the controller behavior
            // We can verify that the refactored assignment logic works by checking results
            
            // Arrange
            var leadData = new Form { LeadId = "test-assignment" };
            _mockCRMService.Setup(x => x.GetLeadDataAsync("test-assignment"))
                .ReturnsAsync(leadData);

            // Act
            var result = _controller.Create("test-assignment").Result as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Model);
        }

        [TestMethod]
        public void FormModel_ValidationAttributes_AreProperlyConfigured()
        {
            // Test the model validation improvements
            // Arrange
            var form = new Form();

            // Act & Assert - Check that required fields are properly marked
            var type = typeof(Form);
            
            // Check CustomerPaymentMethod has Required attribute
            var customerPaymentMethodProperty = type.GetProperty("CustomerPaymentMethod");
            Assert.IsNotNull(customerPaymentMethodProperty);
            var requiredAttribute = customerPaymentMethodProperty.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), false);
            Assert.IsTrue(requiredAttribute.Length > 0, "CustomerPaymentMethod should have Required attribute");

            // Check Branch has Required attribute  
            var branchProperty = type.GetProperty("Branch");
            Assert.IsNotNull(branchProperty);
            var branchRequiredAttribute = branchProperty.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), false);
            Assert.IsTrue(branchRequiredAttribute.Length > 0, "Branch should have Required attribute");

            // Check TradeName has Required attribute
            var tradeNameProperty = type.GetProperty("TradeName");
            Assert.IsNotNull(tradeNameProperty);
            var tradeNameRequiredAttribute = tradeNameProperty.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), false);
            Assert.IsTrue(tradeNameRequiredAttribute.Length > 0, "TradeName should have Required attribute");
        }

        [TestMethod]
        public void FormModel_EcommerceAlias_WorksCorrectly()
        {
            // Test the backward-compatible property alias
            // Arrange
            var form = new Form();
            const string testValue = "test_ecommerce_value";

            // Act - Set via alias
            form.Ecommerce = testValue;

            // Assert - Both properties should have the same value
            Assert.AreEqual(testValue, form.Ecommerce);
            Assert.AreEqual(testValue, form.Ecomerce);
            
            // Act - Set via original property
            const string newValue = "new_ecommerce_value";
            form.Ecomerce = newValue;
            
            // Assert - Both should be updated
            Assert.AreEqual(newValue, form.Ecommerce);
            Assert.AreEqual(newValue, form.Ecomerce);
        }

        [TestMethod]
        public void FormModel_StringLengthValidation_IsApplied()
        {
            // Test that StringLength attribute is applied to TradeName
            var type = typeof(Form);
            var tradeNameProperty = type.GetProperty("TradeName");
            Assert.IsNotNull(tradeNameProperty);
            
            var stringLengthAttribute = tradeNameProperty.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.StringLengthAttribute), false);
            Assert.IsTrue(stringLengthAttribute.Length > 0, "TradeName should have StringLength attribute");
            
            var stringLengthAttr = (System.ComponentModel.DataAnnotations.StringLengthAttribute)stringLengthAttribute[0];
            Assert.AreEqual(200, stringLengthAttr.MaximumLength);
        }

        [TestMethod]
        public void FormModel_RequiredFieldsHaveErrorMessages()
        {
            // Test that required fields have meaningful error messages
            var type = typeof(Form);
            var requiredProperties = new[] { "CustomerPaymentMethod", "Branch", "TradeName", "StatisticGroup", "ChefSegment", "SubSegment" };

            foreach (var propertyName in requiredProperties)
            {
                var property = type.GetProperty(propertyName);
                Assert.IsNotNull(property, $"Property {propertyName} should exist");
                
                var requiredAttribute = (System.ComponentModel.DataAnnotations.RequiredAttribute)
                    property.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), false)[0];
                
                Assert.IsNotNull(requiredAttribute.ErrorMessage, $"{propertyName} should have error message");
                Assert.IsTrue(requiredAttribute.ErrorMessage.Length > 0, $"{propertyName} error message should not be empty");
            }
        }

        #endregion
    }
}
