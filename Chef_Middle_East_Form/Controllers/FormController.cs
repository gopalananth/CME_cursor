using Chef_Middle_East_Form.Models;
using Chef_Middle_East_Form.Services;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Aspose.Words;
using iText.Kernel.Pdf;
using iText.Forms;
using Aspose.Words.Replacing;
using Aspose.Words.Tables;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Diagnostics;
using iTextSharp.text.pdf;
using iTextSharp.text; // For handling PDF documents
using PdfReader = iTextSharp.text.pdf.PdfReader;
using System.Linq;
using PdfWriter = iTextSharp.text.pdf.PdfWriter;
using System.Drawing;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using Microsoft.Xrm.Tooling.Connector;
using System.Configuration;
using System.Net;
using static Chef_Middle_East_Form.Services.FileUploadService;

namespace Chef_Middle_East_Form.Controllers
{
    public class FormController : Controller
    {
        private readonly ICRMService _crmService;

        public FormController(ICRMService crmService)
        {
            _crmService = crmService;
        }


        // GET: Form/Create
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public async Task<ActionResult> Create(string leadId)
        {
            try
            {
                var form = new Form();

                // Validate leadId parameter
                if (string.IsNullOrWhiteSpace(leadId))
                {
                    System.Diagnostics.Trace.WriteLine("Warning: Create method called without leadId");
                    return View("LinkExpired");
                }

                // Validate CRM configuration
                if (!ValidateCRMConfiguration())
                {
                    return View("Error");
                }

                JObject accountData = null;
                Form leadData = null;

                if (!string.IsNullOrEmpty(leadId))
                {
                    // Get Account by lead
                    accountData = await _crmService.GetAccountByLeadId(leadId);

                    // Get Lead data
                    leadData = await _crmService.GetLeadData(leadId);
                    
                    // Check link expiry
                    if (IsLinkExpired(leadData))
                    {
                        return View("LinkExpired");
                    }

                    // Populate form with lead data
                    PopulateFormWithLeadData(form, leadData, leadId);

                    // Populate form with account data if available
                    if (accountData != null)
                    {
                        PopulateFormWithAccountData(form, accountData);
                    }
                }

                return View(form);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in Form/Create: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"Stack trace: {ex.StackTrace}");
                return View("Error");
            }
        }

        /// <summary>
        /// Validates that all required CRM configuration settings are present
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise</returns>
        private bool ValidateCRMConfiguration()
        {
            string crmUrl = ConfigurationManager.AppSettings["CRMAppUrl"];
            string clientId = ConfigurationManager.AppSettings["CRMClientId"];
            string tenantId = ConfigurationManager.AppSettings["CRMTenantId"];
            string clientSecret = ConfigurationManager.AppSettings["CRMClientSecret"];

            if (string.IsNullOrEmpty(crmUrl) || string.IsNullOrEmpty(clientId) || 
                string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientSecret))
            {
                System.Diagnostics.Trace.WriteLine("Error: CRM configuration is incomplete");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if the lead link has expired (48 hours from email sent)
        /// </summary>
        /// <param name="leadData">The lead data containing email sent timestamp</param>
        /// <returns>True if link is expired, false otherwise</returns>
        private bool IsLinkExpired(Form leadData)
        {
            if (leadData?.EmailSenton != null)
            {
                DateTime expiryTime = leadData.EmailSenton.AddHours(48);
                return DateTime.UtcNow > expiryTime;
            }
            return false;
        }

        /// <summary>
        /// Populates the form with basic lead data
        /// </summary>
        /// <param name="form">The form to populate</param>
        /// <param name="leadData">The lead data</param>
        /// <param name="leadId">The lead ID</param>
        private void PopulateFormWithLeadData(Form form, Form leadData, string leadId)
        {
            if (leadData == null) return;

            form.CompanyName = leadData.CompanyName;
            form.MainPhone = leadData.MainPhone;
            form.Email = leadData.Email;
            form.LeadId = leadId;
            form.StatisticGroup = leadData.StatisticGroup;
            form.ChefSegment = leadData.ChefSegment;
            form.SubSegment = leadData.SubSegment;
            form.Branch = leadData.Branch;
            form.Classification = leadData.Classification;
            form.CorporateCustomerName = leadData.CompanyName;
            form.DeliveryCustomerName = leadData.CompanyName;
            form.RegisteredCustomerName = leadData.CompanyName;
            form.Ecomerce = leadData.Ecomerce;
            form.Reason = leadData.Reason;
            form.InventorySystem = leadData.InventorySystem;
            form.StatusCode = leadData.StatusCode;
        }

        /// <summary>
        /// Populates the form with account data from CRM
        /// </summary>
        /// <param name="form">The form to populate</param>
        /// <param name="accountData">The account data from CRM</param>
        private void PopulateFormWithAccountData(Form form, JObject accountData)
        {
            if (accountData == null) return;

            PopulateBasicCompanyInfo(form, accountData);
            PopulateAddressInformation(form, accountData);
            PopulateFinancialInformation(form, accountData);
            PopulateBusinessInformation(form, accountData);
            PopulateBankInformation(form, accountData);
            PopulateFileAttachments(form, accountData);
            PopulateContactInformation(form, accountData);
        }

        /// <summary>
        /// Populates basic company information from account data
        /// </summary>
        private void PopulateBasicCompanyInfo(Form form, JObject accountData)
        {
            if (!string.IsNullOrWhiteSpace(accountData["name"]?.ToString()))
            {
                form.CompanyName = accountData["name"].ToString();
                form.CorporateCustomerName = accountData["name"].ToString();
                form.DeliveryCustomerName = accountData["name"].ToString();
                form.RegisteredCustomerName = accountData["name"].ToString();
            }

            if (!string.IsNullOrWhiteSpace(accountData["telephone1"]?.ToString()))
                form.MainPhone = accountData["telephone1"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["emailaddress1"]?.ToString()))
                form.Email = accountData["emailaddress1"].ToString();
        }

        /// <summary>
        /// Populates address information from account data
        /// </summary>
        private void PopulateAddressInformation(Form form, JObject accountData)
        {
            // Corporate Address
            if (!string.IsNullOrWhiteSpace(accountData["_nw_country11_name"]?.ToString()))
                form.CorporateCountry = accountData["_nw_country11_name"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["_nw_city_name"]?.ToString()))
                form.CorporateCity = accountData["_nw_city_name"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["address1_line1"]?.ToString()))
                form.CorporateStreet = accountData["address1_line1"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["shippingmethodcode"]?.ToString()))
                form.CorporateShippingMethod = accountData["shippingmethodcode"].ToString();

            // Delivery Address
            if (!string.IsNullOrWhiteSpace(accountData["_nw_deliverycountry_name"]?.ToString()))
                form.DeliveryCountry = accountData["_nw_deliverycountry_name"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["_nw_deliverycity_name"]?.ToString()))
                form.DeliveryCity = accountData["_nw_deliverycity_name"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["address2_line1"]?.ToString()))
                form.DeliveryStreet = accountData["address2_line1"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["address2_shippingmethodcode"]?.ToString()))
                form.DeliveryShippingMethod = accountData["address2_shippingmethodcode"].ToString();

            // Registered Address
            if (!string.IsNullOrWhiteSpace(accountData["_nw_countries_name"]?.ToString()))
                form.RegisteredCountry = accountData["_nw_countries_name"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["_nw_cityregisteredaddress_name"]?.ToString()))
                form.RegisteredCity = accountData["_nw_cityregisteredaddress_name"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["nw_address3street"]?.ToString()))
                form.RegisteredStreet = accountData["nw_address3street"].ToString();
        }

        /// <summary>
        /// Populates financial information from account data
        /// </summary>
        private void PopulateFinancialInformation(Form form, JObject accountData)
        {
            if (accountData["nw_proposecreditlimit"] != null)
                form.CreditLimit = accountData["nw_proposecreditlimit"].ToObject<bool>();

            if (!string.IsNullOrWhiteSpace(accountData["nw_proposecreditlimit1"]?.ToString()))
                form.RequestedCreditLimit = accountData["nw_proposecreditlimit1"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["nw_estimatedpurchasevalue"]?.ToString()))
                form.EstimatedPurchaseValue = accountData["nw_estimatedpurchasevalue"].ToString();

            if (accountData["nw_amountofsecuritychequeamountaed"] != null &&
                decimal.TryParse(accountData["nw_amountofsecuritychequeamountaed"].ToString(), out var securityChequeAmt))
                form.SecurityChequeAmount = securityChequeAmt;

            if (accountData["nw_estimatedmonthlypurchaseaed"] != null &&
                decimal.TryParse(accountData["nw_estimatedmonthlypurchaseaed"].ToString(), out var monthlyPurchase))
                form.EstimatedMonthlyPurchase = monthlyPurchase;

            if (!string.IsNullOrWhiteSpace(accountData["nw_proposedpaymentterms"]?.ToString()))
                form.ProposedPaymentTerms = accountData["nw_proposedpaymentterms"].ToString();
        }

        /// <summary>
        /// Populates business information from account data
        /// </summary>
        private void PopulateBusinessInformation(Form form, JObject accountData)
        {
            if (!string.IsNullOrWhiteSpace(accountData["_msdyn_customerpaymentmethod_value@OData.Community.Display.V1.FormattedValue"]?.ToString()))
                form.CustomerPaymentMethod = accountData["_msdyn_customerpaymentmethod_value@OData.Community.Display.V1.FormattedValue"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["_nw_statisticgroup_value@OData.Community.Display.V1.FormattedValue"]?.ToString()))
                form.StatisticGroup = accountData["_nw_statisticgroup_value@OData.Community.Display.V1.FormattedValue"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["_nw_chefsegments_value@OData.Community.Display.V1.FormattedValue"]?.ToString()))
                form.ChefSegment = accountData["_nw_chefsegments_value@OData.Community.Display.V1.FormattedValue"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["_nw_subsegment_value@OData.Community.Display.V1.FormattedValue"]?.ToString()))
                form.SubSegment = accountData["_nw_subsegment_value@OData.Community.Display.V1.FormattedValue"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["_msdyn_company_value@OData.Community.Display.V1.FormattedValue"]?.ToString()))
                form.Branch = accountData["_msdyn_company_value@OData.Community.Display.V1.FormattedValue"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["_shp_classification_value@OData.Community.Display.V1.FormattedValue"]?.ToString()))
                form.Classification = accountData["_shp_classification_value@OData.Community.Display.V1.FormattedValue"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["paymenttermscode@OData.Community.Display.V1.FormattedValue"]?.ToString()))
                form.PaymentTerms = accountData["paymenttermscode@OData.Community.Display.V1.FormattedValue"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["nw_tradenameoutletname"]?.ToString()))
                form.TradeName = accountData["nw_tradenameoutletname"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["nw_tradelicensenumber"]?.ToString()))
                form.TradeLicenseNumber = accountData["nw_tradelicensenumber"].ToString();

            if (accountData["nw_licenseexpirydate"] != null &&
                DateTime.TryParse(accountData["nw_licenseexpirydate"].ToString(), out var licenseExpiry))
                form.LicenseExpiryDate = licenseExpiry;

            if (!string.IsNullOrWhiteSpace(accountData["nw_vatnumber"]?.ToString()))
                form.VatNumber = accountData["nw_vatnumber"].ToString();

            // Boolean dropdowns
            if (accountData["nw_iscontactpersonsameaspurchasing"] != null)
                form.IsContactPersonSameAsPurchasing = (accountData["nw_iscontactpersonsameaspurchasing"].ToObject<bool>()) ? "1" : "0";

            if (accountData["nw_scontactpersonsameascompanyowner"] != null)
                form.IsContactPersonSameAsCompanyOwner = (accountData["nw_scontactpersonsameascompanyowner"].ToObject<bool>()) ? "1" : "0";

            if (accountData["nw_issametocorporateaddress"] != null)
                form.IsSameAsCorporateAddress = (accountData["nw_issametocorporateaddress"].ToObject<bool>()) ? "1" : "0";
        }

        /// <summary>
        /// Populates bank information from account data
        /// </summary>
        private void PopulateBankInformation(Form form, JObject accountData)
        {
            if (!string.IsNullOrWhiteSpace(accountData["bankAccount_nw_name"]?.ToString()))
                form.IBNAccountNumber = accountData["bankAccount_nw_name"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["bankAccount_nw_bankname"]?.ToString()))
                form.BankName = accountData["bankAccount_nw_bankname"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["bankAccount_nw_ibannumber"]?.ToString()))
                form.IbanNumber = accountData["bankAccount_nw_ibannumber"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["bankAccount_nw_swiftcode"]?.ToString()))
                form.SwiftCode = accountData["bankAccount_nw_swiftcode"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["bankAccount_nw_bankaddress"]?.ToString()))
                form.BankAddress = accountData["bankAccount_nw_bankaddress"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["bankAccount_nw_bank"]?.ToString()))
                form.Bank = accountData["bankAccount_nw_bank"].ToString();
        }

        /// <summary>
        /// Populates file attachments from account data
        /// </summary>
        private void PopulateFileAttachments(Form form, JObject accountData)
        {
            string accountID = accountData["accountid"]?.ToString() ?? string.Empty;
            
            form.ChequeCopyFileData = GetFileFromUploadFolder($"checkCopy_{accountID}.pdf");
            form.ChequeCopyFileName = "ChequeCopy.pdf";

            form.VatCertificateFileData = GetFileFromUploadFolder($"vatCertificate_{accountID}.pdf");
            form.VatCertificateFileName = "VATCertificate.pdf";

            form.TradeLicenseFileData = GetFileFromUploadFolder($"tradeLicense_{accountID}.pdf");
            form.TradeLicenseFileName = "TradeLicense.pdf";

            form.POAFileData = GetFileFromUploadFolder($"powerOfAttorney_{accountID}.pdf");
            form.POAFileName = "POA.pdf";

            form.PassportFileData = GetFileFromUploadFolder($"passport_{accountID}.pdf");
            form.PassportFileName = "Passport.pdf";

            form.VisaFileData = GetFileFromUploadFolder($"visa_{accountID}.pdf");
            form.VisaFileName = "Visa.pdf";

            form.EIDFileData = GetFileFromUploadFolder($"emiratesId_{accountID}.pdf");
            form.EIDFileName = "NationalID.pdf";

            form.AccountOpeningFileData = GetFileFromUploadFolder($"accountOpeningFile_{accountID}.pdf");
            form.AccountOpeningFileName = "AccountOpeningFile.pdf";

            form.EstablishmentCardFileData = GetFileFromUploadFolder($"EstablishmentCardCopy_{accountID}.pdf");
            form.EstablishmentCardFileName = "EstablishmentCardCopy.pdf";
        }

        /// <summary>
        /// Populates contact information from account data
        /// </summary>
        private void PopulateContactInformation(Form form, JObject accountData)
        {
            // Person in Charge
            if (!string.IsNullOrWhiteSpace(accountData["primaryContact_firstname"]?.ToString()))
                form.PersonInChargeFirstName = accountData["primaryContact_firstname"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["primaryContact_lastname"]?.ToString()))
                form.PersonInChargeLastName = accountData["primaryContact_lastname"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["primaryContact_emailaddress1"]?.ToString()))
                form.PersonInChargeEmailID = accountData["primaryContact_emailaddress1"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["primaryContact_mobilephone"]?.ToString()))
                form.PersonInChargePhoneNumber = accountData["primaryContact_mobilephone"].ToString();

            // Company Owner
            if (!string.IsNullOrWhiteSpace(accountData["owner_firstname"]?.ToString()))
                form.CompanyOwnerFirstName = accountData["owner_firstname"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["owner_lastname"]?.ToString()))
                form.CompanyOwnerLastName = accountData["owner_lastname"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["owner_emailaddress1"]?.ToString()))
                form.CompanyOwnerEmailID = accountData["owner_emailaddress1"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["owner_mobilephone"]?.ToString()))
                form.CompanyOwnerPhoneNumber = accountData["owner_mobilephone"].ToString();

            // Purchasing Person
            if (!string.IsNullOrWhiteSpace(accountData["purchasingPerson_firstname"]?.ToString()))
                form.PurchasingPersonFirstName = accountData["purchasingPerson_firstname"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["purchasingPerson_lastname"]?.ToString()))
                form.PurchasingPersonLastName = accountData["purchasingPerson_lastname"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["purchasingPerson_emailaddress1"]?.ToString()))
                form.PurchasingPersonEmailID = accountData["purchasingPerson_emailaddress1"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["purchasingPerson_mobilephone"]?.ToString()))
                form.PurchasingPersonPhoneNumber = accountData["purchasingPerson_mobilephone"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["nw_establishmentcardnumber"]?.ToString()))
                form.EstablishmentCardNumber = accountData["nw_establishmentcardnumber"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["primaryContact_mobilephone"]?.ToString()))
                form.PersonInChargePhoneNumber = accountData["primaryContact_mobilephone"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["owner_mobilephone"]?.ToString()))
                form.CompanyOwnerPhoneNumber = accountData["owner_mobilephone"].ToString();

            if (!string.IsNullOrWhiteSpace(accountData["purchasingPerson_mobilephone"]?.ToString()))
                form.PurchasingPersonPhoneNumber = accountData["purchasingPerson_mobilephone"].ToString();
            // ✅ Person in Charge Role
            if (!string.IsNullOrWhiteSpace(accountData["primaryContact_nw_rule"]?.ToString()))
                form.PersonInChargeRole = accountData["primaryContact_nw_rule"].ToString();

            // ✅ Purchasing Person Role
            if (!string.IsNullOrWhiteSpace(accountData["purchasingPerson_nw_rule"]?.ToString()))
                form.PurchasingPersonRole = accountData["purchasingPerson_nw_rule"].ToString();

            // ✅ Company Owner Role
            if (!string.IsNullOrWhiteSpace(accountData["owner_nw_rule"]?.ToString()))
                form.CompanyOwnerRole = accountData["owner_nw_rule"].ToString();


        }
        /// <summary>
        /// Retrieves a file from the upload folder with proper resource management
        /// </summary>
        /// <param name="fileName">Name of the file to retrieve</param>
        /// <returns>File content as byte array, or null if file doesn't exist or error occurs</returns>
        private byte[] GetFileFromUploadFolder(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    System.Diagnostics.Trace.WriteLine("Warning: GetFileFromUploadFolder called with null or empty fileName");
                    return null;
                }

                var uploadFolder = HttpContext.Server.MapPath("~/App_Data/uploads");
                var filePath = Path.Combine(uploadFolder, fileName);

                // Validate file path to prevent directory traversal attacks
                if (!IsValidFilePath(filePath, uploadFolder))
                {
                    System.Diagnostics.Trace.WriteLine($"Warning: Invalid file path detected: {filePath}");
                    return null;
                }

                if (System.IO.File.Exists(filePath))
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var memoryStream = new MemoryStream())
                    {
                        fileStream.CopyTo(memoryStream);
                        return memoryStream.ToArray();
                    }
                }

                return null; // File doesn't exist
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in GetFileFromUploadFolder for file {fileName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates that the file path is within the allowed upload directory
        /// </summary>
        /// <param name="filePath">The file path to validate</param>
        /// <param name="uploadFolder">The allowed upload folder</param>
        /// <returns>True if the path is valid, false otherwise</returns>
        private bool IsValidFilePath(string filePath, string uploadFolder)
        {
            try
            {
                var fullPath = Path.GetFullPath(filePath);
                var uploadFullPath = Path.GetFullPath(uploadFolder);
                return fullPath.StartsWith(uploadFullPath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }


        // POST: Form/Create - Submits form and redirects to Thank You page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Form form, string action)
        {
            try
            {
                // Map uploaded files
                MapUploadedFiles(form);

                // Validate form data
                if (!ValidateFormData(form))
                {
                    return View(form);
                }

                // Check if ModelState is valid
                if (ModelState.IsValid)
                {
                    // Save files to the server with proper error handling
                    var fileResults = await SaveAllFiles(form);
                    
                    if (!fileResults.Success)
                    {
                        ModelState.AddModelError("", fileResults.ErrorMessage);
                        return View(form);
                    }

                    bool accountUpdatedOrCreated = false;

                    // Process based on action type
                    if (action == "Save")
                    {
                        accountUpdatedOrCreated = await ProcessSaveAction(form, fileResults);
                    }
                    else if (action == "Submit")
                    {
                        accountUpdatedOrCreated = await ProcessSubmitAction(form, fileResults);
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid action specified");
                        return View(form);
                    }

                    // Handle success
                    if (accountUpdatedOrCreated)
                    {
                        if (action == "Submit")
                        {
                            Session["SubmittedForm"] = form;
                            return RedirectToAction("Thankyou");
                        }
                        else
                        {
                            TempData["SuccessMessage"] = "Form saved successfully!";
                            return View(form);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to save/update account. Please try again.");
                        return View(form);
                    }
                }

                return View(form);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in Form/Create POST: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View(form);
            }
        }

        /// <summary>
        /// Maps uploaded files from Request.Files to form properties
        /// </summary>
        /// <param name="form">The form to populate with files</param>
        private void MapUploadedFiles(Form form)
        {
            form.POA = Request.Files["POA"];
            form.Passport = Request.Files["Passport"];
            form.Visa = Request.Files["Visa"];
            form.EID = Request.Files["EID"];
            form.AccountOpeningFile = Request.Files["AccountOpeningFile"];
            form.ChequeCopy = Request.Files["ChequeCopy"];
            form.VatCertificate = Request.Files["VatCertificate"];
            form.TradeLicense = Request.Files["TradeLicense"];
            form.EstablishmentCardCopy = Request.Files["EstablishmentCardCopy"];
        }

        /// <summary>
        /// Validates form data before processing
        /// </summary>
        /// <param name="form">The form to validate</param>
        /// <returns>True if validation passes, false otherwise</returns>
        private bool ValidateFormData(Form form)
        {
            if (string.IsNullOrWhiteSpace(form.LeadId))
            {
                ModelState.AddModelError("LeadId", "Lead ID is required");
                return false;
            }

            // Add more validation as needed
            return true;
        }

        /// <summary>
        /// Saves all uploaded files with proper error handling
        /// </summary>
        /// <param name="form">The form containing files to save</param>
        /// <returns>FileSaveResult containing success status and any error messages</returns>
        private async Task<FileSaveResult> SaveAllFiles(Form form)
        {
            try
            {
                string uploadPath = Server.MapPath("~/App_Data/uploads");
                Directory.CreateDirectory(uploadPath);

                var result = new FileSaveResult
                {
                    VatFileData = SaveFile(form.VatCertificate, uploadPath),
                    AccountOpeningFileData = SaveFile(form.AccountOpeningFile, uploadPath),
                    TradeLicenseData = SaveFile(form.TradeLicense, uploadPath),
                    PoaData = SaveFile(form.POA, uploadPath),
                    PassportData = SaveFile(form.Passport, uploadPath),
                    VisaData = SaveFile(form.Visa, uploadPath),
                    EidData = SaveFile(form.EID, uploadPath),
                    ChequeCopyData = SaveFile(form.ChequeCopy, uploadPath),
                    EstablishmentCardData = SaveFile(form.EstablishmentCardCopy, uploadPath),
                    Success = true
                };

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error saving files: {ex.Message}");
                return new FileSaveResult
                {
                    Success = false,
                    ErrorMessage = "Failed to save uploaded files. Please try again."
                };
            }
        }

        /// <summary>
        /// Processes the Save action
        /// </summary>
        /// <param name="form">The form data</param>
        /// <param name="fileResults">The saved file results</param>
        /// <returns>True if successful, false otherwise</returns>
        private async Task<bool> ProcessSaveAction(Form form, FileSaveResult fileResults)
        {
            if (string.IsNullOrEmpty(form.LeadId))
                return false;

            var existingAccount = await _crmService.GetAccountByLeadId(form.LeadId);

            if (existingAccount != null)
            {
                // Update existing account
                return await _crmService.UpdateAccountBasedOnLeadId(
                    form.LeadId, form,
                    fileResults.VatFileData, form.VatCertificate?.FileName,
                    fileResults.TradeLicenseData, form.TradeLicense?.FileName,
                    fileResults.PoaData, form.POA?.FileName,
                    fileResults.PassportData, form.Passport?.FileName,
                    fileResults.VisaData, form.Visa?.FileName,
                    fileResults.EidData, form.EID?.FileName,
                    fileResults.AccountOpeningFileData, form.AccountOpeningFile?.FileName,
                    fileResults.ChequeCopyData, form.ChequeCopy?.FileName,
                    fileResults.EstablishmentCardData, form.EstablishmentCardCopy?.FileName, false
                );
            }
            else
            {
                // Create new account
                return await _crmService.CreateAccountInCRM(
                    form,
                    fileResults.VatFileData, form.VatCertificate?.FileName,
                    fileResults.TradeLicenseData, form.TradeLicense?.FileName,
                    fileResults.PoaData, form.POA?.FileName,
                    fileResults.PassportData, form.Passport?.FileName,
                    fileResults.VisaData, form.Visa?.FileName,
                    fileResults.EidData, form.EID?.FileName,
                    fileResults.AccountOpeningFileData, form.AccountOpeningFile?.FileName,
                    fileResults.ChequeCopyData, form.ChequeCopy?.FileName,
                    fileResults.EstablishmentCardData, form.EstablishmentCardCopy?.FileName
                );
            }
        }

        /// <summary>
        /// Processes the Submit action
        /// </summary>
        /// <param name="form">The form data</param>
        /// <param name="fileResults">The saved file results</param>
        /// <returns>True if successful, false otherwise</returns>
        private async Task<bool> ProcessSubmitAction(Form form, FileSaveResult fileResults)
        {
            if (string.IsNullOrEmpty(form.LeadId))
                return false;

            var existingAccount = await _crmService.GetAccountByLeadId(form.LeadId);

            if (existingAccount == null)
            {
                // Create the account first
                var accountCreated = await _crmService.CreateAccountInCRM(
                    form,
                    fileResults.VatFileData, form.VatCertificate?.FileName,
                    fileResults.TradeLicenseData, form.TradeLicense?.FileName,
                    fileResults.PoaData, form.POA?.FileName,
                    fileResults.PassportData, form.Passport?.FileName,
                    fileResults.VisaData, form.Visa?.FileName,
                    fileResults.EidData, form.EID?.FileName,
                    fileResults.AccountOpeningFileData, form.AccountOpeningFile?.FileName,
                    fileResults.ChequeCopyData, form.ChequeCopy?.FileName,
                    fileResults.EstablishmentCardData, form.EstablishmentCardCopy?.FileName
                );

                if (!accountCreated)
                    return false;

                // Wait for account to be available with retry logic
                existingAccount = await WaitForAccountAvailability(form.LeadId);
                if (existingAccount == null)
                    return false;
            }

            // Update the account
            return await _crmService.UpdateAccountBasedOnLeadId(
                form.LeadId, form,
                fileResults.VatFileData, form.VatCertificate?.FileName,
                fileResults.TradeLicenseData, form.TradeLicense?.FileName,
                fileResults.PoaData, form.POA?.FileName,
                fileResults.PassportData, form.Passport?.FileName,
                fileResults.VisaData, form.Visa?.FileName,
                fileResults.EidData, form.EID?.FileName,
                fileResults.AccountOpeningFileData, form.AccountOpeningFile?.FileName,
                fileResults.ChequeCopyData, form.ChequeCopy?.FileName,
                fileResults.EstablishmentCardData, form.EstablishmentCardCopy?.FileName, true
            );
        }

        /// <summary>
        /// Waits for account to become available after creation
        /// </summary>
        /// <param name="leadId">The lead ID to check</param>
        /// <returns>The account data if found, null otherwise</returns>
        private async Task<JObject> WaitForAccountAvailability(string leadId)
        {
            int retryCount = 0;
            const int maxRetries = 10;
            const int retryDelayMs = 1000;

            while (retryCount < maxRetries)
            {
                await Task.Delay(retryDelayMs);
                var existingAccount = await _crmService.GetAccountByLeadId(leadId);
                if (existingAccount != null)
                    return existingAccount;

                retryCount++;
            }

            System.Diagnostics.Trace.WriteLine($"Account not available after {maxRetries} retries for lead ID: {leadId}");
            return null;
        }

        /// <summary>
        /// Result class for file save operations
        /// </summary>
        private class FileSaveResult
        {
            public byte[] VatFileData { get; set; }
            public byte[] AccountOpeningFileData { get; set; }
            public byte[] TradeLicenseData { get; set; }
            public byte[] PoaData { get; set; }
            public byte[] PassportData { get; set; }
            public byte[] VisaData { get; set; }
            public byte[] EidData { get; set; }
            public byte[] ChequeCopyData { get; set; }
            public byte[] EstablishmentCardData { get; set; }
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
        }


        private byte[] SaveFile(HttpPostedFileBase file, string path)
        {
            if (file != null && file.ContentLength > 0)
            {
                // File size validation (10MB limit)
                const int maxFileSizeBytes = 10 * 1024 * 1024; // 10MB
                if (file.ContentLength > maxFileSizeBytes)
                {
                    throw new ArgumentException($"File size exceeds maximum allowed size of 10MB. Current size: {file.ContentLength / (1024 * 1024):F2}MB");
                }

                // File type validation
                string[] allowedExtensions = { ".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg" };
                string fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                
                if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                {
                    throw new ArgumentException($"File type not allowed. Allowed types: {string.Join(", ", allowedExtensions)}. Received: {fileExtension}");
                }

                // Sanitize filename to prevent path traversal attacks
                string safeFileName = Path.GetFileName(file.FileName);
                if (string.IsNullOrEmpty(safeFileName) || safeFileName.Contains("..") || safeFileName.Contains("/") || safeFileName.Contains("\\"))
                {
                    throw new ArgumentException("Invalid filename detected.");
                }

                string filePath = Path.Combine(path, safeFileName);
                
                // Ensure the target directory is within the uploads folder
                string uploadsFolder = Server.MapPath("~/App_Data/uploads");
                if (!filePath.StartsWith(uploadsFolder, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Invalid file path detected.");
                }

                try
                {
                    file.SaveAs(filePath);
                    
                    using (var binaryReader = new BinaryReader(file.InputStream))
                    {
                        return binaryReader.ReadBytes(file.ContentLength);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error and rethrow
                    System.Diagnostics.Trace.WriteLine($"Error saving file {safeFileName}: {ex.Message}");
                    throw new InvalidOperationException($"Failed to save file: {ex.Message}", ex);
                }
            }
            return null;
        }


        // GET: Form/ThankYou - Displays thank you page and triggers CRM creation
        public ActionResult Thankyou()
        {
            Session.Remove("SubmittedForm"); // Only clear session, don't create an account again
            return View();
        }

        public ActionResult ThankyouOnSave()
        {
            Session.Remove("SubmittedForm"); // Only clear session, don't create an account again
            return View();
        }

        // 🚀 NEW: API endpoint to create CRM account on button click from CRM lead form
        [HttpPost]
        public async Task<JsonResult> CreateAccount(Form form)
        {
            if (form != null)
            {
                var result = await _crmService.CreateAccountInCRM(form, 
                    null, null,
                    null,null,
                    null,null,
                    null, null,
                    null, null,
                    null, null,
                    null, null,
                    null, null,
                    null,null);
                return Json(new { success = result });
            }
            return Json(new { success = false, message = "Invalid form data." });
        }
        /// <summary>
        /// Generates a PDF preview of the form before submission
        /// </summary>
        /// <param name="form">The form collection containing form data</param>
        /// <returns>PDF file result or error status</returns>
        [HttpPost]
        public ActionResult GeneratePdfBeforeSubmit(FormCollection form)
        {
            try
            {
                // Validate template exists
                string templatePath = Server.MapPath("~/App_Data/Templates/Account Credit Form 1.0.docx");
                if (!System.IO.File.Exists(templatePath))
                {
                    return new HttpStatusCodeResult(404, "Template not found.");
                }

                // Set up Aspose license
                SetupAsposeLicense();

                // Load document
                var doc = new Aspose.Words.Document(templatePath);

                // Process form data
                var formValues = BuildFormValuesDictionary(form);
                var roleMapping = BuildRoleMappingDictionary();

                // Process contact information
                ProcessContactInformation(doc, form, formValues, roleMapping);

                // Process attachments
                ProcessAttachments(doc, form);

                // Process form fields
                ProcessFormFields(doc, form, formValues);

                // Process address fields
                ProcessAddressFields(doc, form);

                // Hide watermarks
                HideWatermarks(doc);

                // Generate and return PDF
                return GeneratePdfResult(doc);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error generating PDF: {ex.Message}");
                return new HttpStatusCodeResult(500, "Failed to generate PDF. Please try again.");
            }
        }

        /// <summary>
        /// Sets up the Aspose.Words license
        /// </summary>
        private void SetupAsposeLicense()
        {
            byte[] bytes = Chef_Middle_East_Form.Properties.Resources.Aspose_Words_NET;
            using (var stream = new MemoryStream(bytes))
            {
                var license = new Aspose.Words.License();
                license.SetLicense(stream);
            }
        }

        /// <summary>
        /// Builds a dictionary of form values for PDF generation
        /// </summary>
        /// <param name="form">The form collection</param>
        /// <returns>Dictionary of form field names and values</returns>
        private Dictionary<string, string> BuildFormValuesDictionary(FormCollection form)
        {
            string businessCategory = $"{form["StatisticGroup"]}\n{form["ChefSegment"]}\n{form["SubSegment"]}";

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "company name (as per trade license)", form["CompanyName"] },
                { "trade name/outlet name", form["TradeName"] },
                { "vat – trn #", form["VatNumber"] },
                { "registered address_name", form["CompanyName"] },
                { "license expiry date", form["LicenseExpiryDate"] },
                { "trade license number", form["TradeLicenseNumber"] },
                { "security cheque amount", form["SecurityChequeAmount"] },
                { "payment terms", form["PaymentTerms"] },
                { "currency in which account to be operated", "UAE Dirham" },
                { "method of payment", form["CustomerPaymentMethod"] },
                { "estimated purchase value", form["EstimatedPurchaseValue"] },
                { "proposed credit limit", form["RequestedCreditLimit"] },
                { "estimated monthly purchase", form["EstimatedMonthlyPurchase"] },
                { "bank name", form["BankName"] },
                { "account number", form["IBNAccountNumber"] },
                { "iban number", form["IbanNumber"] },
                { "swift code", form["SwiftCode"] },
                { "correspondent bank name (if any)", form["Bank"] },
                { "bank address", form["BankAddress"] },
                { "proposed payment terms", form["ProposedPaymentTerms"] }
            };
        }

        /// <summary>
        /// Builds the role mapping dictionary for converting numeric roles to text
        /// </summary>
        /// <returns>Dictionary mapping role codes to role names</returns>
        private Dictionary<string, string> BuildRoleMappingDictionary()
        {
            return new Dictionary<string, string>
            {
                { "266990000", "Beverage professionals" },
                { "266990001", "Culinary professionals" },
                { "266990002", "Executive level" },
                { "266990003", "Finance" },
                { "266990004", "Operations" },
                { "266990005", "Pastry professionals" },
                { "266990006", "Purchase" },
                { "266990007", "Sponsor" },
                { "0", "Reference" },
                { "1", "Owner" },
                { "2", "Other" },
                { "266990008", "Ecommerce" }
            };
        }

        /// <summary>
        /// Processes contact information in the document
        /// </summary>
        /// <param name="doc">The document to process</param>
        /// <param name="form">The form collection</param>
        /// <param name="formValues">The form values dictionary</param>
        /// <param name="roleMapping">The role mapping dictionary</param>
        private void ProcessContactInformation(Aspose.Words.Document doc, FormCollection form, 
            Dictionary<string, string> formValues, Dictionary<string, string> roleMapping)
        {
            // Process Person in Charge details
            ProcessPersonInCharge(doc, form, formValues, roleMapping);

            // Process Purchasing Person details
            ProcessPurchasingPerson(doc, form, formValues, roleMapping);

            // Process Company Owner details
            ProcessCompanyOwner(doc, form, formValues, roleMapping);
        }

        /// <summary>
        /// Processes Person in Charge information
        /// </summary>
        private void ProcessPersonInCharge(Aspose.Words.Document doc, FormCollection form, 
            Dictionary<string, string> formValues, Dictionary<string, string> roleMapping)
        {
            string personInChargeFullName = $"{form["PersonInChargeFirstName"]} {form["PersonInChargeLastName"]}".Trim();
            if (!string.IsNullOrWhiteSpace(personInChargeFullName))
                formValues.Add("person in charge full name", personInChargeFullName);

            if (!string.IsNullOrWhiteSpace(form["PersonInChargeEmailID"]))
                formValues.Add("person in charge contact email", form["PersonInChargeEmailID"]);

            if (!string.IsNullOrWhiteSpace(form["PersonInChargePhoneNumber"]))
                formValues.Add("person in charge mobile phone", form["PersonInChargePhoneNumber"]);

            string personInChargeRole = form["PersonInChargeRole"];
            if (roleMapping.ContainsKey(personInChargeRole))
                personInChargeRole = roleMapping[personInChargeRole];

            if (!string.IsNullOrWhiteSpace(personInChargeRole))
                formValues.Add("person in charge role", personInChargeRole);
        }

        /// <summary>
        /// Processes Purchasing Person information
        /// </summary>
        private void ProcessPurchasingPerson(Aspose.Words.Document doc, FormCollection form, 
            Dictionary<string, string> formValues, Dictionary<string, string> roleMapping)
        {
            string personInChargeFullName = $"{form["PersonInChargeFirstName"]} {form["PersonInChargeLastName"]}".Trim();

            string purchasingPersonFullName = $"{form["PurchasingPersonFirstName"]} {form["PurchasingPersonLastName"]}".Trim();
            if (string.IsNullOrWhiteSpace(purchasingPersonFullName))
                purchasingPersonFullName = personInChargeFullName;

            if (!string.IsNullOrWhiteSpace(purchasingPersonFullName))
                formValues.Add("purchasing person full name", purchasingPersonFullName);

            string purchasingPersonEmail = form["PurchasingPersonEmailID"];
            if (string.IsNullOrWhiteSpace(purchasingPersonEmail))
                purchasingPersonEmail = form["PersonInChargeEmailID"];

            if (!string.IsNullOrWhiteSpace(purchasingPersonEmail))
                formValues.Add("purchasing person contact email", purchasingPersonEmail);

            string purchasingPersonPhone = RemoveKnownCountryCodes(form["PurchasingPersonPhoneNumber"]);
            if (string.IsNullOrWhiteSpace(purchasingPersonPhone))
                purchasingPersonPhone = form["PersonInChargePhoneNumber"];

            if (!string.IsNullOrWhiteSpace(purchasingPersonPhone))
                formValues.Add("purchasing person mobile phone", purchasingPersonPhone);

            string purchasingPersonRole = form["PurchasingPersonRole"];
            if (string.IsNullOrWhiteSpace(purchasingPersonRole))
                purchasingPersonRole = form["PersonInChargeRole"];

            if (roleMapping.ContainsKey(purchasingPersonRole))
                purchasingPersonRole = roleMapping[purchasingPersonRole];

            if (!string.IsNullOrWhiteSpace(purchasingPersonRole))
                formValues.Add("purchasing person role", purchasingPersonRole);
        }

        /// <summary>
        /// Processes Company Owner information
        /// </summary>
        private void ProcessCompanyOwner(Aspose.Words.Document doc, FormCollection form, 
            Dictionary<string, string> formValues, Dictionary<string, string> roleMapping)
        {
            string personInChargeFullName = $"{form["PersonInChargeFirstName"]} {form["PersonInChargeLastName"]}".Trim();

            string companyOwnerFullName = $"{form["CompanyOwnerFirstName"]} {form["CompanyOwnerLastName"]}".Trim();
            if (string.IsNullOrWhiteSpace(companyOwnerFullName))
                companyOwnerFullName = personInChargeFullName;

            if (!string.IsNullOrWhiteSpace(companyOwnerFullName))
                formValues.Add("company owner full name", companyOwnerFullName);

            string companyOwnerEmail = form["CompanyOwnerEmailID"];
            if (string.IsNullOrWhiteSpace(companyOwnerEmail))
                companyOwnerEmail = form["PersonInChargeEmailID"];

            if (!string.IsNullOrWhiteSpace(companyOwnerEmail))
                formValues.Add("company owner contact email", companyOwnerEmail);

            string companyOwnerPhone = RemoveKnownCountryCodes(form["CompanyOwnerPhoneNumber"]);
            if (string.IsNullOrWhiteSpace(companyOwnerPhone))
                companyOwnerPhone = form["PersonInChargePhoneNumber"];

            if (!string.IsNullOrWhiteSpace(companyOwnerPhone))
                formValues.Add("company owner mobile phone", companyOwnerPhone);

            string companyOwnerRole = form["CompanyOwnerRole"];
            if (string.IsNullOrWhiteSpace(companyOwnerRole))
                companyOwnerRole = form["PersonInChargeRole"];

            if (roleMapping.ContainsKey(companyOwnerRole))
                companyOwnerRole = roleMapping[companyOwnerRole];

            if (!string.IsNullOrWhiteSpace(companyOwnerRole))
                formValues.Add("company owner role", companyOwnerRole);
        }

        /// <summary>
        /// Processes attachments in the document
        /// </summary>
        /// <param name="doc">The document to process</param>
        /// <param name="form">The form collection</param>
        private void ProcessAttachments(Aspose.Words.Document doc, FormCollection form)
        {
            var attachments = new Dictionary<string, string>
            {
                { "VAT - TRN #", "VatCertificate" },
                { "Trade/Commercial License", "TradeLicense" },
                { "Visa", "Visa" },
                { "Passport", "Passport" },
                { "Power of Attorney", "POA" },
                { "National ID", "EID" },
                { "Account Opening File", "AccountOpeningFile" },
                { "Cheque Copy", "ChequeCopy" }
            };

            var availableAttachments = attachments
                .Where(a => Request.Files[a.Value] != null && Request.Files[a.Value].ContentLength > 0)
                .Select(a => "• " + a.Key)
                .ToList();

            if (availableAttachments.Any())
            {
                string attachmentText = string.Join("\n", availableAttachments);
                Trace.WriteLine("Attachments to insert:\n" + attachmentText);

                InsertAttachmentsIntoDocument(doc, attachmentText);
            }
            else
            {
                Trace.WriteLine("⚠️ No valid attachments found.");
            }
        }

        /// <summary>
        /// Inserts attachments list into the document
        /// </summary>
        /// <param name="doc">The document to modify</param>
        /// <param name="attachmentText">The attachment text to insert</param>
        private void InsertAttachmentsIntoDocument(Aspose.Words.Document doc, string attachmentText)
        {
            foreach (Aspose.Words.Paragraph para in doc.GetChildNodes(NodeType.Paragraph, true))
            {
                string paraText = para.GetText().Trim().ToLower();

                if (paraText.Contains("list of attachments"))
                {
                    Trace.WriteLine("✅ Found 'List of Attachments' paragraph.");

                    var newParagraph = new Aspose.Words.Paragraph(doc);
                    newParagraph.AppendChild(new Run(doc, attachmentText));

                    para.ParentNode.InsertAfter(newParagraph, para);
                    break;
                }
            }
        }

        /// <summary>
        /// Processes form fields in the document
        /// </summary>
        /// <param name="doc">The document to process</param>
        /// <param name="form">The form collection</param>
        /// <param name="formValues">The form values dictionary</param>
        private void ProcessFormFields(Aspose.Words.Document doc, FormCollection form, Dictionary<string, string> formValues)
        {
            foreach (Table table in doc.GetChildNodes(NodeType.Table, true))
            {
                foreach (Row row in table.Rows)
                {
                    if (row.Cells.Count < 2)
                    {
                        Trace.WriteLine("Skipping row due to insufficient cells: " + row.GetText());
                        continue;
                    }

                    string cellText = row.Cells[0].GetText()
                        .Trim()
                        .Replace("\r", "")
                        .Replace("\n", "")
                        .Replace(":", "")
                        .ToLower();

                    int valueColumnIndex = (row.Cells.Count == 2) ? 1 : row.Cells.Count - 1;

                    ProcessFormFieldRow(doc, row, cellText, valueColumnIndex, form, formValues);
                }
            }
        }

        /// <summary>
        /// Processes a single form field row
        /// </summary>
        private void ProcessFormFieldRow(Aspose.Words.Document doc, Row row, string cellText, 
            int valueColumnIndex, FormCollection form, Dictionary<string, string> formValues)
        {
            if (cellText.Contains("business category"))
            {
                ProcessBusinessCategoryRow(doc, row, valueColumnIndex, form);
            }
            else if (cellText == "name")
            {
                ProcessNameRow(doc, row, valueColumnIndex, form);
            }
            else if (formValues.ContainsKey(cellText))
            {
                ProcessFormValueRow(doc, row, valueColumnIndex, formValues[cellText]);
            }
        }

        /// <summary>
        /// Processes business category row
        /// </summary>
        private void ProcessBusinessCategoryRow(Aspose.Words.Document doc, Row row, int valueColumnIndex, FormCollection form)
        {
            string businessCategory = $"{form["StatisticGroup"]}\n{form["ChefSegment"]}\n{form["SubSegment"]}";
            
            row.Cells[valueColumnIndex].RemoveAllChildren();
            string[] categories = businessCategory.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string line in categories)
            {
                var para = new Aspose.Words.Paragraph(doc);
                para.AppendChild(new Run(doc, line.Trim()));
                row.Cells[valueColumnIndex].AppendChild(para);
            }
        }

        /// <summary>
        /// Processes name row
        /// </summary>
        private void ProcessNameRow(Aspose.Words.Document doc, Row row, int valueColumnIndex, FormCollection form)
        {
            string parentTableText = row.GetAncestor(NodeType.Table).Range.Text.ToLower();
            if (parentTableText.Contains("registered address") || parentTableText.Contains("corporate address") || parentTableText.Contains("delivery address"))
            {
                row.Cells[valueColumnIndex].RemoveAllChildren();
                var para = new Aspose.Words.Paragraph(doc);
                para.AppendChild(new Run(doc, form["CompanyName"]));
                row.Cells[valueColumnIndex].AppendChild(para);
            }
        }

        /// <summary>
        /// Processes form value row
        /// </summary>
        private void ProcessFormValueRow(Aspose.Words.Document doc, Row row, int valueColumnIndex, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                row.Cells[valueColumnIndex].RemoveAllChildren();
                var para = new Aspose.Words.Paragraph(doc);
                para.AppendChild(new Run(doc, value));
                row.Cells[valueColumnIndex].AppendChild(para);
            }
        }

        /// <summary>
        /// Processes address fields in the document
        /// </summary>
        /// <param name="doc">The document to process</param>
        /// <param name="form">The form collection</param>
        private void ProcessAddressFields(Aspose.Words.Document doc, FormCollection form)
        {
            foreach (Table table in doc.GetChildNodes(NodeType.Table, true))
            {
                foreach (Row row in table.Rows)
                {
                    string cellText = row.Cells[0].GetText()
                        .Trim()
                        .Replace("\r", "")
                        .Replace("\n", "")
                        .Replace(":", "")
                        .ToLower();

                    if (cellText == "country" || cellText == "city" || cellText == "street")
                    {
                        ProcessAddressRow(doc, row, cellText, form);
                    }
                }
            }
        }

        /// <summary>
        /// Processes a single address row
        /// </summary>
        private void ProcessAddressRow(Aspose.Words.Document doc, Row row, string cellText, FormCollection form)
        {
            string section = GetAddressSection(row);
            string valueToSet = GetAddressValue(section, cellText, form);

            if (!string.IsNullOrWhiteSpace(valueToSet))
            {
                if (row.Cells.Count >= 2)
                {
                    // Normal two-column row
                    row.Cells[1].RemoveAllChildren();
                    var para = new Aspose.Words.Paragraph(doc);
                    para.AppendChild(new Run(doc, valueToSet));
                    row.Cells[1].AppendChild(para);
                }
                else
                {
                    // Single-cell row: update the first cell of the next row
                    UpdateNextRowCell(doc, row, valueToSet);
                }
            }
        }

        /// <summary>
        /// Gets the address value based on section and field type
        /// </summary>
        private string GetAddressValue(string section, string fieldType, FormCollection form)
        {
            switch (section)
            {
                case "registered":
                    return fieldType switch
                    {
                        "country" => !string.IsNullOrWhiteSpace(form["RegisteredCountry"]) ? form["RegisteredCountry"] : form["DeliveryCountry"],
                        "city" => !string.IsNullOrWhiteSpace(form["RegisteredCity"]) ? form["RegisteredCity"] : form["DeliveryCity"],
                        "street" => !string.IsNullOrWhiteSpace(form["RegisteredStreet"]) ? form["RegisteredStreet"] : form["DeliveryStreet"],
                        _ => null
                    };
                case "corporate":
                    return fieldType switch
                    {
                        "country" => form["CorporateCountry"],
                        "city" => form["CorporateCity"],
                        "street" => form["CorporateStreet"],
                        _ => null
                    };
                case "delivery":
                    return fieldType switch
                    {
                        "country" => form["DeliveryCountry"],
                        "city" => form["DeliveryCity"],
                        "street" => form["DeliveryStreet"],
                        _ => null
                    };
                default:
                    return null;
            }
        }

        /// <summary>
        /// Updates the next row's cell with the address value
        /// </summary>
        private void UpdateNextRowCell(Aspose.Words.Document doc, Row currentRow, string value)
        {
            Table parentTable = (Table)currentRow.GetAncestor(NodeType.Table);
            int currentIndex = parentTable.Rows.IndexOf(currentRow);
            
            if (currentIndex + 1 < parentTable.Rows.Count)
            {
                Row nextRow = parentTable.Rows[currentIndex + 1];
                if (nextRow.Cells.Count > 0)
                {
                    nextRow.Cells[0].RemoveAllChildren();
                    var para = new Aspose.Words.Paragraph(doc);
                    para.AppendChild(new Run(doc, value));
                    nextRow.Cells[0].AppendChild(para);
                }
            }
        }

        /// <summary>
        /// Hides watermarks in the document
        /// </summary>
        /// <param name="doc">The document to process</param>
        private void HideWatermarks(Aspose.Words.Document doc)
        {
            foreach (Aspose.Words.Paragraph para in doc.GetChildNodes(NodeType.Paragraph, true))
            {
                string text = para.GetText().Trim();
                if (text.Contains("Evaluation Only") || text.Contains("Created with an evaluation copy of Aspose.Words"))
                {
                    foreach (Run run in para.Runs)
                    {
                        run.Font.Color = System.Drawing.Color.White;
                    }
                }
            }
        }

        /// <summary>
        /// Generates the final PDF result
        /// </summary>
        /// <param name="doc">The processed document</param>
        /// <returns>PDF file result or error status</returns>
        private ActionResult GeneratePdfResult(Aspose.Words.Document doc)
        {
            using (var stream = new MemoryStream())
            {
                doc.Save(stream, SaveFormat.Pdf);
                stream.Position = 0;
                return File(stream.ToArray(), "application/pdf", "Account_Opening_Form_Preview.pdf");
            }
        }

        /// <summary>
        /// Removes known country codes from phone numbers
        /// </summary>
        /// <param name="phoneNumber">The phone number to process</param>
        /// <returns>Phone number without country code</returns>
        private string RemoveKnownCountryCodes(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            phoneNumber = phoneNumber.Trim();

            var countryCodes = new[] { "+971", "+968", "+974" };

            foreach (var code in countryCodes)
            {
                if (phoneNumber.StartsWith(code))
                {
                    return phoneNumber.Substring(code.Length).TrimStart();
                }
            }

            return phoneNumber;
        }

        public ActionResult LinkExpired()
        {
            return View();
        }

        private string GetAddressSection(Row currentRow)
        {
            Table parentTable = (Table)currentRow.GetAncestor(NodeType.Table);
            int currentIndex = parentTable.Rows.IndexOf(currentRow);

            for (int i = currentIndex - 1; i >= 0 && i >= currentIndex - 5; i--) // Check up to 5 rows above
            {
                Row labelRow = parentTable.Rows[i];
                string labelText = labelRow.GetText().ToLower();

                if (labelText.Contains("registered address")) return "registered";
                if (labelText.Contains("corporate address")) return "corporate";
                if (labelText.Contains("delivery address")) return "delivery";
            }

            return null;
        }


    }
}
