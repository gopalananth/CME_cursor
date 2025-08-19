using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
        
        private readonly int _maxFileSizeBytes;

        public FileUploadService()
        {
            // Make max file size configurable with proper error handling
            try
            {
                var maxFileSizeMBString = ConfigurationManager.AppSettings["MaxFileSizeMB"] ?? "10";
                var maxFileSizeMB = int.Parse(maxFileSizeMBString);
                
                // Validate reasonable bounds (1MB to 100MB)
                if (maxFileSizeMB < 1 || maxFileSizeMB > 100)
                {
                    throw new ArgumentOutOfRangeException($"MaxFileSizeMB must be between 1 and 100, got: {maxFileSizeMB}");
                }
                
                _maxFileSizeBytes = maxFileSizeMB * 1024 * 1024;
            }
            catch (Exception ex) when (!(ex is ArgumentOutOfRangeException))
            {
                // Log the error and use safe default
                System.Diagnostics.Trace.WriteLine($"Error parsing MaxFileSizeMB configuration, using default 10MB: {ex.Message}");
                _maxFileSizeBytes = 10 * 1024 * 1024; // 10MB default
            }
        }

        public FileValidationResult ValidateFile(HttpPostedFileBase file)
        {
            if (file == null)
                return new FileValidationResult { IsValid = false, ErrorMessage = "No file was uploaded." };

            if (file.ContentLength == 0)
                return new FileValidationResult { IsValid = false, ErrorMessage = "The uploaded file is empty." };

            if (file.ContentLength > _maxFileSizeBytes)
                return new FileValidationResult { IsValid = false, ErrorMessage = $"File size exceeds the maximum limit of {_maxFileSizeBytes / (1024 * 1024)}MB." };

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

        public async Task<byte[]> SaveFileAsync(HttpPostedFileBase file, string uploadPath)
        {
            var validationResult = ValidateFile(file);
            if (!validationResult.IsValid)
                throw new ArgumentException(validationResult.ErrorMessage);

            return await Task.Run(() =>
            {
                using (var binaryReader = new BinaryReader(file.InputStream))
                {
                    return binaryReader.ReadBytes(file.ContentLength);
                }
            });
        }

        public int GetMaxFileSize()
        {
            return _maxFileSizeBytes;
        }

        // Note: Removed dead code UploadFileToCrm() method that was not used anywhere
        // This method was incomplete and posed security risks with hardcoded configuration
    }
}