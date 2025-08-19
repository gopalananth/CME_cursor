using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chef_Middle_East_Form.Services;

namespace Chef_Middle_East_Form.Tests
{
    [TestClass]
    public class SecureConfigurationServiceTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            // Clean up environment variables after each test
            Environment.SetEnvironmentVariable("TEST_ENV_VAR", null);
            Environment.SetEnvironmentVariable("CRM_APP_URL", null);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", null);
        }

        [TestMethod]
        public void GetSecureValue_EnvironmentVariableExists_ReturnsEnvironmentValue()
        {
            // Arrange
            const string key = "TEST_ENV_VAR";
            const string expectedValue = "env_value_123";
            Environment.SetEnvironmentVariable(key, expectedValue);

            // Act
            var result = SecureConfigurationService.GetSecureValue(key, "fallback_key", "fallback_value");

            // Assert
            Assert.AreEqual(expectedValue, result);
        }

        [TestMethod]
        public void GetSecureValue_EnvironmentVariableEmpty_FallsBackToAppSettings()
        {
            // Arrange
            const string key = "NonExistentEnvVar";
            const string appSettingsKey = "UnobtrusiveJavaScriptEnabled"; // This exists in web.config
            
            // Act
            var result = SecureConfigurationService.GetSecureValue(key, appSettingsKey, "fallback_value");

            // Assert
            Assert.AreEqual("true", result); // Value from web.config
        }

        [TestMethod]
        public void GetSecureValue_BothSourcesEmpty_ReturnsFallback()
        {
            // Arrange
            const string key = "NonExistentKey";
            const string appSettingsKey = "NonExistentAppSetting";
            const string fallbackValue = "fallback_test_value";

            // Act
            var result = SecureConfigurationService.GetSecureValue(key, appSettingsKey, fallbackValue);

            // Assert
            Assert.AreEqual(fallbackValue, result);
        }

        [TestMethod]
        public void GetSecureValue_NoAppSettingsKey_UsesSameKeyForBoth()
        {
            // Arrange
            const string key = "ClientValidationEnabled"; // This exists in web.config
            const string fallbackValue = "fallback_value";

            // Act
            var result = SecureConfigurationService.GetSecureValue(key, null, fallbackValue);

            // Assert
            Assert.AreEqual("true", result); // Value from web.config using same key
        }

        [TestMethod]
        public void GetSecureValue_AllSourcesNull_ReturnsNull()
        {
            // Arrange
            const string key = "NonExistentKey";
            const string appSettingsKey = "NonExistentAppSetting";

            // Act
            var result = SecureConfigurationService.GetSecureValue(key, appSettingsKey, null);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetCRMClientSecret_NoSecretConfigured_ThrowsException()
        {
            // Arrange - No environment variable or app setting for client secret
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", null);

            // Act & Assert - Should throw InvalidOperationException
            SecureConfigurationService.GetCRMClientSecret();
        }

        [TestMethod]
        public void GetCRMClientSecret_EnvironmentVariableSet_ReturnsSecret()
        {
            // Arrange
            const string testSecret = "test_secret_123";
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", testSecret);

            // Act
            var result = SecureConfigurationService.GetCRMClientSecret();

            // Assert
            Assert.AreEqual(testSecret, result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetCRMConfiguration_MissingAppUrl_ThrowsException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", "test_secret");
            // CRMAppUrl is required but will be missing/empty

            // Act & Assert
            SecureConfigurationService.GetCRMConfiguration();
        }

        [TestMethod]
        public void GetCRMConfiguration_ValidConfiguration_ReturnsConfig()
        {
            // Arrange
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", "test_secret_456");
            // Other values come from web.config

            // Act
            var config = SecureConfigurationService.GetCRMConfiguration();

            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual("test_secret_456", config.ClientSecret);
            Assert.IsNotNull(config.AppUrl);
            Assert.IsNotNull(config.ClientId);
            Assert.IsNotNull(config.TenantId);
        }

        [TestMethod]
        public void CRMConfiguration_GetConnectionString_ReturnsValidConnectionString()
        {
            // Arrange
            var config = new CRMConfiguration
            {
                AppUrl = "https://test.crm.dynamics.com",
                ClientId = "test-client-id",
                TenantId = "test-tenant-id",
                ClientSecret = "test-secret"
            };

            // Act
            var connectionString = config.GetConnectionString();

            // Assert
            Assert.IsTrue(connectionString.Contains("AuthType=ClientSecret"));
            Assert.IsTrue(connectionString.Contains("Url=https://test.crm.dynamics.com"));
            Assert.IsTrue(connectionString.Contains("ClientId=test-client-id"));
            Assert.IsTrue(connectionString.Contains("TenantId=test-tenant-id"));
            Assert.IsTrue(connectionString.Contains("ClientSecret=test-secret"));
        }

        [TestMethod]
        public void ValidateSecureConfiguration_ValidConfig_ReturnsValidResult()
        {
            // Arrange
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", "test_secret_789");

            // Act
            var result = SecureConfigurationService.ValidateSecureConfiguration();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Messages.Count > 0);
            Assert.IsTrue(result.Messages[0].Contains("✅"));
        }

        [TestMethod]
        public void ValidateSecureConfiguration_InvalidConfig_ReturnsInvalidResult()
        {
            // Arrange
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", null);

            // Act
            var result = SecureConfigurationService.ValidateSecureConfiguration();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Messages.Count > 0);
            Assert.IsTrue(result.Messages[0].Contains("❌"));
        }

        [TestMethod]
        public void ConfigurationValidationResult_DefaultConstructor_InitializesCorrectly()
        {
            // Act
            var result = new ConfigurationValidationResult();

            // Assert
            Assert.IsFalse(result.IsValid); // Default should be false
            Assert.IsNotNull(result.Messages);
            Assert.AreEqual(0, result.Messages.Count);
        }

        [TestMethod]
        public void GetSecureValue_ExceptionInAccess_ReturnsFailback()
        {
            // This test verifies exception handling in configuration access
            // Arrange
            const string fallbackValue = "fallback_on_exception";

            // Act - Using a very long key that might cause issues
            var longKey = new string('x', 1000);
            var result = SecureConfigurationService.GetSecureValue(longKey, null, fallbackValue);

            // Assert
            Assert.AreEqual(fallbackValue, result);
        }
    }
}