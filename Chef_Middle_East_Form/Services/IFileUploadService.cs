using System.Threading.Tasks;
using System.Web;

namespace Chef_Middle_East_Form.Services
{
    public interface IFileUploadService
    {
        FileValidationResult ValidateFile(HttpPostedFileBase file);
        byte[] SaveFile(HttpPostedFileBase file, string uploadPath);
        Task<byte[]> SaveFileAsync(HttpPostedFileBase file, string uploadPath);
        int GetMaxFileSize();
    }

    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }
}

