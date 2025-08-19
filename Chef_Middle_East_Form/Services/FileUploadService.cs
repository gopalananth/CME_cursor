using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Chef_Middle_East_Form.Services
{
    public class FileUploadService : IFileUploadService
    {
        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg" };
        private static readonly string[] AllowedMimeTypes = { 
            "application/pdf", 
            "application/msword", 
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "image/png", 
            "image/jpeg", 
            "image/jpg" 
        };
        private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

        public FileValidationResult ValidateFile(HttpPostedFileBase file)
        {
            if (file == null)
                return new FileValidationResult { IsValid = false, ErrorMessage = "No file was uploaded." };

            if (file.ContentLength == 0)
                return new FileValidationResult { IsValid = false, ErrorMessage = "The uploaded file is empty." };

            if (file.ContentLength > MaxFileSizeBytes)
                return new FileValidationResult { IsValid = false, ErrorMessage = "File size exceeds the maximum limit of 10MB." };

            if (string.IsNullOrWhiteSpace(file.FileName))
                return new FileValidationResult { IsValid = false, ErrorMessage = "Invalid filename." };

            // Check for path traversal attempts
            if (file.FileName.Contains("..") || file.FileName.Contains("/") || file.FileName.Contains("\\") || file.FileName.Contains("\0"))
                return new FileValidationResult { IsValid = false, ErrorMessage = "Invalid filename contains illegal characters." };

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return new FileValidationResult { IsValid = false, ErrorMessage = "File type is not allowed." };

            if (!AllowedMimeTypes.Contains(file.ContentType))
                return new FileValidationResult { IsValid = false, ErrorMessage = "File type is not allowed." };

            return new FileValidationResult { IsValid = true, ErrorMessage = null };
        }

        public byte[] SaveFile(HttpPostedFileBase file, string uploadPath)
        {
            var validationResult = ValidateFile(file);
            if (!validationResult.IsValid)
                throw new ArgumentException(validationResult.ErrorMessage);

            using (var binaryReader = new BinaryReader(file.InputStream))
            {
                return binaryReader.ReadBytes(file.ContentLength);
            }
        }

        public static void UploadFileToCrm()
        {
            string crmUrl = ConfigurationManager.AppSettings["CRMAppUrl"];
            string clientId = ConfigurationManager.AppSettings["CRMClientId"];
            string tenantId = ConfigurationManager.AppSettings["CRMTenantId"];
            string clientSecret = ConfigurationManager.AppSettings["CRMClientSecret"];

            string connectionString = $"AuthType=ClientSecret;Url={crmUrl};ClientId={clientId};ClientSecret={clientSecret};TenantId={tenantId}";

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var serviceClient = new ServiceClient(connectionString);

            if (serviceClient.IsReady)
            {
                var st = "Connected Successfully";
            }
        }
    }
}