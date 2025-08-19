using System;
using System.Configuration;

namespace Chef_Middle_East_Form.Services
{
    /// <summary>
    /// Service for securely retrieving configuration values from multiple sources
    /// Implements security best practices for sensitive configuration data
    /// </summary>
    public class SecureConfigurationService
    {
        /// <summary>
        /// Gets a configuration value securely from environment variables first, then falls back to app settings
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="appSettingsKey">App settings key if different from environment variable key</param>
        /// <param name="fallbackValue">Fallback value if not found in either source</param>
        /// <returns>Configuration value</returns>
        /// <remarks>
        /// This method implements the security best practice of:
        /// 1. First checking environment variables (secure for production)
        /// 2. Then checking app settings (for development/testing)
        /// 3. Using fallback value if neither is available
        /// </remarks>
        public static string GetSecureValue(string key, string appSettingsKey = null, string fallbackValue = null)
        {
            try
            {
                // 1. First priority: Environment variables (most secure for production)
                var envValue = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrEmpty(envValue))
                {
                    return envValue;
                }

                // 2. Second priority: App settings (for development/testing)
                var appSettingsValue = ConfigurationManager.AppSettings[appSettingsKey ?? key];
                if (!string.IsNullOrEmpty(appSettingsValue))
                {
                    return appSettingsValue;
                }

                // 3. Fallback value
                return fallbackValue;
            }
            catch (Exception ex)
            {
                // Log the error but don't expose sensitive information
                System.Diagnostics.Trace.WriteLine($"Error retrieving secure configuration for key '{key}': {ex.Message}");
                return fallbackValue;
            }
        }

        /// <summary>
        /// Gets the CRM client secret securely from environment variables
        /// </summary>
        /// <returns>Client secret or throws exception if not found</returns>
        /// <exception cref="InvalidOperationException">Thrown when client secret is not configured</exception>
        public static string GetCRMClientSecret()
        {
            var clientSecret = GetSecureValue("AZURE_CLIENT_SECRET", "CRMClientSecret");
            
            if (string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException(
                    "CRM Client Secret not configured. Please set the AZURE_CLIENT_SECRET environment variable " +
                    "or configure CRMClientSecret in app settings for development environments.");
            }

            return clientSecret;
        }

        /// <summary>
        /// Gets CRM connection configuration with secure secret handling
        /// </summary>
        /// <returns>CRM configuration object</returns>
        public static CRMConfiguration GetCRMConfiguration()
        {
            return new CRMConfiguration
            {
                AppUrl = GetSecureValue("CRM_APP_URL", "CRMAppUrl") ?? 
                        throw new InvalidOperationException("CRM App URL not configured"),
                        
                ClientId = GetSecureValue("CRM_CLIENT_ID", "CRMClientId") ?? 
                          throw new InvalidOperationException("CRM Client ID not configured"),
                          
                TenantId = GetSecureValue("CRM_TENANT_ID", "CRMTenantId") ?? 
                          throw new InvalidOperationException("CRM Tenant ID not configured"),
                          
                ClientSecret = GetCRMClientSecret()
            };
        }

        /// <summary>
        /// Validates that all required secure configuration values are available
        /// </summary>
        /// <returns>Validation result with details</returns>
        public static ConfigurationValidationResult ValidateSecureConfiguration()
        {
            var result = new ConfigurationValidationResult { IsValid = true };

            try
            {
                var config = GetCRMConfiguration();
                result.Messages.Add("✅ CRM configuration is valid and secure");
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Messages.Add($"❌ CRM configuration error: {ex.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// CRM configuration data structure
    /// </summary>
    public class CRMConfiguration
    {
        public string AppUrl { get; set; }
        public string ClientId { get; set; }
        public string TenantId { get; set; }
        public string ClientSecret { get; set; }
        
        /// <summary>
        /// Gets the connection string for CRM authentication
        /// </summary>
        public string GetConnectionString()
        {
            return $"AuthType=ClientSecret;Url={AppUrl};ClientId={ClientId};ClientSecret={ClientSecret};TenantId={TenantId}";
        }
    }

    /// <summary>
    /// Configuration validation result
    /// </summary>
    public class ConfigurationValidationResult
    {
        public bool IsValid { get; set; }
        public System.Collections.Generic.List<string> Messages { get; set; } = new System.Collections.Generic.List<string>();
    }
}