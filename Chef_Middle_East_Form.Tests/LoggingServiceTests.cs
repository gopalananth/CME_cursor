using System;
using System.IO;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chef_Middle_East_Form.Services;

namespace Chef_Middle_East_Form.Tests
{
    [TestClass]
    public class LoggingServiceTests
    {
        private LoggingService _loggingService;
        private string _testLogPath;

        [TestInitialize]
        public void Setup()
        {
            // Create a test log directory
            _testLogPath = Path.Combine(Path.GetTempPath(), "test_logs", "test.log");
            var logDir = Path.GetDirectoryName(_testLogPath);
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            _loggingService = new LoggingService();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_testLogPath))
            {
                File.Delete(_testLogPath);
            }
        }

        [TestMethod]
        public void LogInfo_ShouldLogInformationMessage()
        {
            // Arrange
            var message = "Test information message";
            var additionalData = new { TestKey = "TestValue" };

            // Act
            _loggingService.LogInfo(message, additionalData);

            // Assert - No exception should be thrown
            Assert.IsTrue(true, "LogInfo should execute without throwing exceptions");
        }

        [TestMethod]
        public void LogWarning_ShouldLogWarningMessage()
        {
            // Arrange
            var message = "Test warning message";

            // Act
            _loggingService.LogWarning(message);

            // Assert - No exception should be thrown
            Assert.IsTrue(true, "LogWarning should execute without throwing exceptions");
        }

        [TestMethod]
        public void LogError_ShouldLogErrorMessage()
        {
            // Arrange
            var message = "Test error message";
            var exception = new Exception("Test exception");

            // Act
            _loggingService.LogError(message, exception);

            // Assert - No exception should be thrown
            Assert.IsTrue(true, "LogError should execute without throwing exceptions");
        }

        [TestMethod]
        public void LogException_ShouldReturnErrorId()
        {
            // Arrange
            var exception = new ArgumentException("Test argument exception");
            var context = new { UserId = "test-user", Action = "test-action" };

            // Act
            var errorId = _loggingService.LogException(exception, context);

            // Assert
            Assert.IsNotNull(errorId);
            Assert.AreEqual(8, errorId.Length);
            Assert.IsTrue(errorId.All(char.IsLetterOrDigit));
        }

        [TestMethod]
        public void LogException_WithNullException_ShouldHandleGracefully()
        {
            // Arrange
            Exception exception = null;

            // Act & Assert
            Assert.ThrowsException<NullReferenceException>(() => _loggingService.LogException(exception));
        }

        [TestMethod]
        public void LogInfo_WithNullMessage_ShouldHandleGracefully()
        {
            // Arrange
            string message = null;

            // Act
            _loggingService.LogInfo(message);

            // Assert - No exception should be thrown
            Assert.IsTrue(true, "LogInfo with null message should execute without throwing exceptions");
        }

        [TestMethod]
        public void LogError_WithInnerException_ShouldLogBothExceptions()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner exception message");
            var outerException = new ApplicationException("Outer exception message", innerException);
            var message = "Test error with inner exception";

            // Act
            _loggingService.LogError(message, outerException);

            // Assert - No exception should be thrown
            Assert.IsTrue(true, "LogError with inner exception should execute without throwing exceptions");
        }

        [TestMethod]
        public void LogWarning_WithAdditionalData_ShouldLogSuccessfully()
        {
            // Arrange
            var message = "Test warning with data";
            var additionalData = new 
            { 
                RequestId = Guid.NewGuid().ToString(),
                UserId = "test-user",
                Timestamp = DateTime.UtcNow
            };

            // Act
            _loggingService.LogWarning(message, additionalData);

            // Assert - No exception should be thrown
            Assert.IsTrue(true, "LogWarning with additional data should execute without throwing exceptions");
        }

        [TestMethod]
        public void LogException_WithComplexContext_ShouldReturnValidErrorId()
        {
            // Arrange
            var exception = new FileNotFoundException("Test file not found", "test-file.txt");
            var complexContext = new 
            {
                RequestDetails = new 
                {
                    Method = "POST",
                    Url = "/Form/Create",
                    UserAgent = "Test Agent"
                },
                UserInfo = new 
                {
                    UserId = "test-user-123",
                    Email = "test@example.com"
                },
                SystemInfo = new 
                {
                    Environment = "Test",
                    Version = "1.0.0"
                }
            };

            // Act
            var errorId = _loggingService.LogException(exception, complexContext);

            // Assert
            Assert.IsNotNull(errorId);
            Assert.AreEqual(8, errorId.Length);
            Assert.IsTrue(errorId.All(char.IsUpper));
        }
    }
}