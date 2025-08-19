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
        private Mock<HttpContextBase> _mockHttpContext;
        private Mock<HttpServerUtilityBase> _mockServer;

        [TestInitialize]
        public void Setup()
        {
            _mockCRMService = new Mock<ICRMService>();
            _mockFileUploadService = new Mock<IFileUploadService>();
            _mockHttpContext = new Mock<HttpContextBase>();
            _mockServer = new Mock<HttpServerUtilityBase>();

            _mockHttpContext.Setup(x => x.Server).Returns(_mockServer.Object);
            _mockHttpContext.Setup(x => x.Session).Returns(new Mock<HttpSessionStateBase>().Object);

            _controller = new FormController(_mockCRMService.Object)
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
    }
}
