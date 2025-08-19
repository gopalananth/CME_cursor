using System;
using System.Threading.Tasks;
using Chef_Middle_East_Form.Models;
using Newtonsoft.Json.Linq;

namespace Chef_Middle_East_Form.Services
{
    public interface ICRMService
    {
        // Account creation methods
        Task<string> CreateAccountAsync(Form formData);
        Task<bool> CreateAccountInCRM(
            Form model,
            byte[] vatFileData, string vatFileName,
            byte[] poaData, string poaFileName,
            byte[] Passport, string PassportFileName,
            byte[] Visa, string VisaFileName,
            byte[] EID, string EIDFileName,
            byte[] tradeLicenseData, string tradeLicenseName,
            byte[] accountOpeningFileData, string accountOpeningFileName,
            byte[] chequefiledata, string chequefilename,
            byte[] EstablishmentCardFileData, string EstablishmentCardFileName);
            
        // Lead data methods
        Task<Form> GetLeadData(string leadId);
        Task<Form> GetLeadDataAsync(string leadId);

        // Account data methods
        Task<JObject> GetAccountByLeadId(string leadId);
        Task<JObject> GetAccountDataAsync(string accountId);
        
        // Update methods
        Task<bool> UpdateAccountAsync(string accountId, Form formData);
        Task<bool> UpdateAccountBasedOnLeadId(
            string leadId,
            Form model,
            byte[] vatFileData, string vatFileName,
            byte[] tradeLicenseData, string tradeLicenseName,
            byte[] poaData, string poaFileName,
            byte[] passportData, string passportFileName,
            byte[] visaData, string visaFileName,
            byte[] eidData, string eidFileName,
            byte[] accountOpeningFileData, string accountOpeningFileName,
            byte[] chequeFileData, string chequeFileName,
            byte[] EstablishmentCardFileData, string EstablishmentCardFileName, bool canUpdateLeadStatus);
            
        // Utility methods
        string GetUploadedFileNameByService(string accountId, string fileattachmentid);


    }
}