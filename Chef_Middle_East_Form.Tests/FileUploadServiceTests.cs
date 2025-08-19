using System;
using System.IO;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Chef_Middle_East_Form.Services;

namespace Chef_Middle_East_Form.Tests
{
    [TestClass]
    public class FileUploadServiceTests
    {
        private IFileUploadService _fileUploadService;
        private Mock<HttpContextBase> _mockHttpContext;
        private Mock<HttpServerUtilityBase> _mockServer;

        [TestInitialize]
        public void Setup()
        {
            _mockHttpContext = new Mock<HttpContextBase>();
            _mockServer = new Mock<HttpServerUtilityBase>();
            _mockHttpContext.Setup(x => x.Server).Returns(_mockServer.Object);
            
            _fileUploadService = new FileUploadService();
        }

        #region File Validation Tests

        [TestMethod]
        public void ValidateFile_ValidPdfFile_ShouldReturnTrue()
        {
            // Arrange
            var mockFile = CreateMockFile("test.pdf", "application/pdf", 1024);

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsNull(result.ErrorMessage);
        }

        [TestMethod]
        public void ValidateFile_ValidDocFile_ShouldReturnTrue()
        {
            // Arrange
            var mockFile = CreateMockFile("document.doc", "application/msword", 2048);

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsNull(result.ErrorMessage);
        }

        [TestMethod]
        public void ValidateFile_ValidDocxFile_ShouldReturnTrue()
        {
            // Arrange
            var mockFile = CreateMockFile("document.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 3072);

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsNull(result.ErrorMessage);
        }

        [TestMethod]
        public void ValidateFile_ValidImageFile_ShouldReturnTrue()
        {
            // Arrange
            var mockFile = CreateMockFile("image.png", "image/png", 512);

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsNull(result.ErrorMessage);
        }

        [TestMethod]
        public void ValidateFile_FileSizeExceedsLimit_ShouldReturnFalse()
        {
            // Arrange
            var mockFile = CreateMockFile("large.pdf", "application/pdf", 15 * 1024 * 1024); // 15MB

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsTrue(result.ErrorMessage.Contains("10MB"));
        }

        [TestMethod]
        public void ValidateFile_InvalidFileType_ShouldReturnFalse()
        {
            // Arrange
            var mockFile = CreateMockFile("script.exe", "application/octet-stream", 1024);

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsTrue(result.ErrorMessage.Contains("not allowed"));
        }

        [TestMethod]
        public void ValidateFile_NullFile_ShouldReturnFalse()
        {
            // Arrange
            HttpPostedFileBase mockFile = null;

            // Act
            var result = _fileUploadService.ValidateFile(mockFile);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [TestMethod]
        public void ValidateFile_EmptyFile_ShouldReturnFalse()
        {
            // Arrange
            var mockFile = CreateMockFile("empty.pdf", "application/pdf", 0);

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.ErrorMessage);
        }

        #endregion

        #region File Security Tests

        [TestMethod]
        public void ValidateFile_PathTraversalAttempt_ShouldReturnFalse()
        {
            // Arrange
            var mockFile = CreateMockFile("../../../malicious.txt", "text/plain", 1024);

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsTrue(result.ErrorMessage.Contains("Invalid filename"));
        }

        [TestMethod]
        public void ValidateFile_FileNameWithSlashes_ShouldReturnFalse()
        {
            // Arrange
            var mockFile = CreateMockFile("folder/file.pdf", "application/pdf", 1024);

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsTrue(result.ErrorMessage.Contains("Invalid filename"));
        }

        [TestMethod]
        public void ValidateFile_FileNameWithBackslashes_ShouldReturnFalse()
        {
            // Arrange
            var mockFile = CreateMockFile("folder\\file.pdf", "application/pdf", 1024);

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsTrue(result.ErrorMessage.Contains("Invalid filename"));
        }

        [TestMethod]
        public void ValidateFile_FileNameWithNullBytes_ShouldReturnFalse()
        {
            // Arrange
            var mockFile = CreateMockFile("file\0.pdf", "application/pdf", 1024);

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.ErrorMessage);
        }

        #endregion

        #region File Extension Tests

        [TestMethod]
        public void ValidateFile_UpperCaseExtension_ShouldReturnTrue()
        {
            // Arrange
            var mockFile = CreateMockFile("document.PDF", "application/pdf", 1024);

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsNull(result.ErrorMessage);
        }

        [TestMethod]
        public void ValidateFile_MixedCaseExtension_ShouldReturnTrue()
        {
            // Arrange
            var mockFile = CreateMockFile("document.PdF", "application/pdf", 1024);

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsNull(result.ErrorMessage);
        }

        [TestMethod]
        public void ValidateFile_NoExtension_ShouldReturnFalse()
        {
            // Arrange
            var mockFile = CreateMockFile("document", "application/octet-stream", 1024);

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.ErrorMessage);
        }

        #endregion

        #region File Size Boundary Tests

        [TestMethod]
        public void ValidateFile_FileSizeAtLimit_ShouldReturnTrue()
        {
            // Arrange
            var mockFile = CreateMockFile("test.pdf", "application/pdf", 10 * 1024 * 1024); // Exactly 10MB

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsNull(result.ErrorMessage);
        }

        [TestMethod]
        public void ValidateFile_FileSizeJustBelowLimit_ShouldReturnTrue()
        {
            // Arrange
            var mockFile = CreateMockFile("test.pdf", "application/pdf", (10 * 1024 * 1024) - 1); // Just under 10MB

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsNull(result.ErrorMessage);
        }

        [TestMethod]
        public void ValidateFile_FileSizeJustAboveLimit_ShouldReturnFalse()
        {
            // Arrange
            var mockFile = CreateMockFile("test.pdf", "application/pdf", (10 * 1024 * 1024) + 1); // Just over 10MB

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.ErrorMessage);
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

        #endregion

        #region New Async and Configuration Tests

        [TestMethod]
        public void GetMaxFileSize_ShouldReturnConfiguredValue()
        {
            // Act
            var maxSize = _fileUploadService.GetMaxFileSize();

            // Assert
            Assert.IsTrue(maxSize > 0, "Max file size should be greater than 0");
            // Default should be 10MB (10 * 1024 * 1024 bytes)
            Assert.AreEqual(10 * 1024 * 1024, maxSize);
        }

        [TestMethod]
        public async Task SaveFileAsync_ValidFile_ShouldReturnByteArray()
        {
            // Arrange
            var testData = new byte[] { 1, 2, 3, 4, 5 };
            var mockFile = CreateMockFileWithData("test.pdf", "application/pdf", testData);

            // Act
            var result = await _fileUploadService.SaveFileAsync(mockFile.Object, "test-path");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(testData.Length, result.Length);
            CollectionAssert.AreEqual(testData, result);
        }

        [TestMethod]
        public async Task SaveFileAsync_InvalidFile_ShouldThrowException()
        {
            // Arrange
            var mockFile = CreateMockFile("test.exe", "application/exe", 1024);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
                _fileUploadService.SaveFileAsync(mockFile.Object, "test-path"));
        }

        [TestMethod]
        public async Task SaveFileAsync_NullFile_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
                _fileUploadService.SaveFileAsync(null, "test-path"));
        }

        [TestMethod]
        public async Task SaveFileAsync_FileTooLarge_ShouldThrowException()
        {
            // Arrange
            var largeSize = 15 * 1024 * 1024; // 15MB (larger than default 10MB limit)
            var mockFile = CreateMockFile("large.pdf", "application/pdf", largeSize);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
                _fileUploadService.SaveFileAsync(mockFile.Object, "test-path"));
        }

        [TestMethod]
        public void ValidateFile_FileAtMaxSizeLimit_ShouldReturnTrue()
        {
            // Arrange
            var maxSize = _fileUploadService.GetMaxFileSize();
            var mockFile = CreateMockFile("atmax.pdf", "application/pdf", maxSize);

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsNull(result.ErrorMessage);
        }

        [TestMethod]
        public void ValidateFile_FileOverMaxSizeLimit_ShouldReturnFalse()
        {
            // Arrange
            var maxSize = _fileUploadService.GetMaxFileSize();
            var mockFile = CreateMockFile("overlimit.pdf", "application/pdf", maxSize + 1);

            // Act
            var result = _fileUploadService.ValidateFile(mockFile.Object);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsTrue(result.ErrorMessage.Contains("exceeds"));
        }

        private Mock<HttpPostedFileBase> CreateMockFileWithData(string fileName, string contentType, byte[] data)
        {
            var mockFile = new Mock<HttpPostedFileBase>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.ContentType).Returns(contentType);
            mockFile.Setup(f => f.ContentLength).Returns(data.Length);
            
            var stream = new MemoryStream(data);
            mockFile.Setup(f => f.InputStream).Returns(stream);
            
            return mockFile;
        }

        #endregion
    }
}
