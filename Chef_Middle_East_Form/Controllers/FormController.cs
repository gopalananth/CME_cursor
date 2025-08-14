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

                string crmUrl = ConfigurationManager.AppSettings["CRMAppUrl"];
                string clientId = ConfigurationManager.AppSettings["CRMClientId"];
                string tenantId = ConfigurationManager.AppSettings["CRMTenantId"];
                string clientSecret = ConfigurationManager.AppSettings["CRMClientSecret"];

                // Validate configuration
                if (string.IsNullOrEmpty(crmUrl) || string.IsNullOrEmpty(clientId) || 
                    string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientSecret))
                {
                    System.Diagnostics.Trace.WriteLine("Error: CRM configuration is incomplete");
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
                if (leadData?.EmailSenton != null)
                {
                    DateTime expiryTime = leadData.EmailSenton.AddHours(48);

                    if (DateTime.UtcNow > expiryTime)
                    {
                        return View("LinkExpired");
                    }
                }

                // Use Lead data as fallback if Account data is unavailable
                form.CompanyName = leadData?.CompanyName;
                form.MainPhone = leadData?.MainPhone;
                form.Email = leadData?.Email;
                form.LeadId = leadId;
                form.StatisticGroup = leadData?.StatisticGroup;
                form.ChefSegment = leadData?.ChefSegment;
                form.SubSegment = leadData?.SubSegment;
                form.Branch = leadData?.Branch;
                form.Classification = leadData?.Classification;
                form.CorporateCustomerName = leadData?.CompanyName;
                form.DeliveryCustomerName = leadData?.CompanyName;
                form.RegisteredCustomerName = leadData?.CompanyName;
                form.Ecomerce = leadData?.Ecomerce;
                form.Reason = leadData?.Reason;
                form.InventorySystem = leadData?.InventorySystem;
                form.StatusCode = leadData?.StatusCode;

                if (accountData != null)
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

                    if (!string.IsNullOrWhiteSpace(accountData["_nw_country11_name"]?.ToString()))
                        form.CorporateCountry = accountData["_nw_country11_name"].ToString();

                    if (!string.IsNullOrWhiteSpace(accountData["_nw_city_name"]?.ToString()))
                        form.CorporateCity = accountData["_nw_city_name"].ToString();

                    if (!string.IsNullOrWhiteSpace(accountData["_nw_deliverycountry_name"]?.ToString()))
                        form.DeliveryCountry = accountData["_nw_deliverycountry_name"].ToString();

                    if (!string.IsNullOrWhiteSpace(accountData["_nw_deliverycity_name"]?.ToString()))
                        form.DeliveryCity = accountData["_nw_deliverycity_name"].ToString();

                    if (!string.IsNullOrWhiteSpace(accountData["_nw_countries_name"]?.ToString()))
                        form.RegisteredCountry = accountData["_nw_countries_name"].ToString();

                    if (!string.IsNullOrWhiteSpace(accountData["_nw_cityregisteredaddress_name"]?.ToString()))
                        form.RegisteredCity = accountData["_nw_cityregisteredaddress_name"].ToString();

                    if (!string.IsNullOrWhiteSpace(accountData["address1_line1"]?.ToString()))
                        form.CorporateStreet = accountData["address1_line1"].ToString();

                    if (!string.IsNullOrWhiteSpace(accountData["shippingmethodcode"]?.ToString()))
                        form.CorporateShippingMethod = accountData["shippingmethodcode"].ToString();

                    if (!string.IsNullOrWhiteSpace(accountData["address2_line1"]?.ToString()))
                        form.DeliveryStreet = accountData["address2_line1"].ToString();

                    if (!string.IsNullOrWhiteSpace(accountData["address2_shippingmethodcode"]?.ToString()))
                        form.DeliveryShippingMethod = accountData["address2_shippingmethodcode"].ToString();

                    if (!string.IsNullOrWhiteSpace(accountData["nw_address3street"]?.ToString()))
                        form.RegisteredStreet = accountData["nw_address3street"].ToString();

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

                    // Bank Account Details
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

                    if (!string.IsNullOrWhiteSpace(accountData["nw_proposedpaymentterms"]?.ToString()))
                        form.ProposedPaymentTerms = accountData["nw_proposedpaymentterms"].ToString();

                    // File Attachments
                    string accountID = accountData["accountid"]?.ToString() ?? string.Empty;
                    form.ChequeCopyFileData = GetFileFromUploadFolder($"checkCopy_{accountID}.pdf");
                    form.ChequeCopyFileName = "ChequeCopy.pdf"; // _crmService.GetUploadedFileNameByService(accountID, accountData["nw_checkcopy"].ToString()); //"ChequeCopy.pdf";

                    form.VatCertificateFileData = GetFileFromUploadFolder($"vatCertificate_{accountID}.pdf");
                    form.VatCertificateFileName = "VATCertificate.pdf";//_crmService.GetUploadedFileNameByService(accountID, accountData["nw_vattrnattachcertificate"].ToString()); //"VATCertificate.pdf";

                    form.TradeLicenseFileData = GetFileFromUploadFolder($"tradeLicense_{accountID}.pdf");
                    form.TradeLicenseFileName = "TradeLicense.pdf";// _crmService.GetUploadedFileNameByService(accountID, accountData["nw_tradecommerciallicensenoattachlicense"].ToString()); //"TradeLicense.pdf";

                    form.POAFileData = GetFileFromUploadFolder($"powerOfAttorney_{accountID}.pdf");
                    form.POAFileName = "POA.pdf"; // _crmService.GetUploadedFileNameByService(accountID, accountData["nw_powerofattorney"].ToString()); //"POA.pdf";

                    form.PassportFileData = GetFileFromUploadFolder($"passport_{accountID}.pdf");
                    form.PassportFileName = "Passport.pdf"; //_crmService.GetUploadedFileNameByService(accountID, accountData["nw_passport"].ToString()); //""Passport.pdf";

                    form.VisaFileData = GetFileFromUploadFolder($"visa_{accountID}.pdf");
                    form.VisaFileName = "Visa.pdf";// _crmService.GetUploadedFileNameByService(accountID, accountData["nw_visa"].ToString()); //"Visa.pdf";

                    form.EIDFileData = GetFileFromUploadFolder($"emiratesId_{accountID}.pdf");
                    form.EIDFileName = "NationalID.pdf"; // _crmService.GetUploadedFileNameByService(accountID, accountData["nw_emiratesidcard"].ToString()); //"NationalID.pdf";

                    form.AccountOpeningFileData = GetFileFromUploadFolder($"accountOpeningFile_{accountID}.pdf");
                    form.AccountOpeningFileName = "AccountOpeningFile.pdf";

                    form.EstablishmentCardFileData = GetFileFromUploadFolder($"EstablishmentCardCopy_{accountID}.pdf");
                    form.EstablishmentCardFileName = "EstablishmentCardCopy.pdf"; //_crmService.GetUploadedFileNameByService(accountID, accountData["nw_establishmentcardcopy"].ToString()); //"EstablishmentCardCopy.pdf";

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
        private byte[] GetFileFromUploadFolder(string fileName)
        {
            var uploadFolder = HttpContext.Server.MapPath("~/App_Data/uploads");
            var filePath = Path.Combine(uploadFolder, fileName);

            if (System.IO.File.Exists(filePath))
            {
                return System.IO.File.ReadAllBytes(filePath);  // Return the file as a byte array
            }

            return null;  // Return null if the file does not exist
        }



        // POST: Form/Create - Submits form and redirects to Thank You page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Form form, string action)
        {
            form.POA = Request.Files["POA"];
            form.Passport = Request.Files["Passport"];
            form.Visa = Request.Files["Visa"];
            form.EID = Request.Files["EID"];
            form.AccountOpeningFile = Request.Files["AccountOpeningFile"];
            form.ChequeCopy = Request.Files["ChequeCopy"];
            form.VatCertificate = Request.Files["VatCertificate"];
            form.TradeLicense = Request.Files["TradeLicense"];
            form.EstablishmentCardCopy= Request.Files["EstablishmentCardCopy"];


            //// Debug log to check the received files
            //System.Diagnostics.Trace.WriteLine("===== Received Files After Mapping =====");
            //System.Diagnostics.Trace.WriteLine($"POA: {form.POA?.FileName ?? "NULL"}");
            //System.Diagnostics.Trace.WriteLine($"Passport: {form.Passport?.FileName ?? "NULL"}");
            //System.Diagnostics.Trace.WriteLine($"Visa: {form.Visa?.FileName ?? "NULL"}");
            //System.Diagnostics.Trace.WriteLine($"EID: {form.EID?.FileName ?? "NULL"}");
            //System.Diagnostics.Trace.WriteLine($"Account Opening File: {form.AccountOpeningFile?.FileName ?? "NULL"}");
            //System.Diagnostics.Trace.WriteLine($"Cheque Copy: {form.ChequeCopy?.FileName ?? "NULL"}");
            //System.Diagnostics.Trace.WriteLine($"Establoshment Card Copy: {form.EstablishmentCardCopy?.FileName ?? "NULL"}");
            //System.Diagnostics.Trace.WriteLine("========================================");

            // Check if ModelState is valid
            if (ModelState.IsValid)
            {
                // Save files to the server
                string uploadPath = Server.MapPath("~/App_Data/uploads");
                Directory.CreateDirectory(uploadPath);

                byte[] vatFileData = SaveFile(form.VatCertificate, uploadPath);
                byte[] accountOpeningFileData = SaveFile(form.AccountOpeningFile, uploadPath);
                byte[] tradeLicenseData = SaveFile(form.TradeLicense, uploadPath);
                byte[] poaData = SaveFile(form.POA, uploadPath);
                byte[] Passport = SaveFile(form.Passport, uploadPath);
                byte[] Visa = SaveFile(form.Visa, uploadPath);
                byte[] EID = SaveFile(form.EID, uploadPath);
                byte[] ChequeCopy = SaveFile(form.ChequeCopy, uploadPath);
                byte[] EstablishmentCard = SaveFile(form.EstablishmentCardCopy, uploadPath);


                bool accountUpdatedOrCreated = false;

                // Check which button was clicked (Save or Submit)
                if (action == "Save")
                {
                    if (!string.IsNullOrEmpty(form.LeadId))
                    {
                        var existingAccount = await _crmService.GetAccountByLeadId(form.LeadId);

                        if (existingAccount != null)
                        {
                            // Update the existing account instead of creating a new one
                            accountUpdatedOrCreated = await _crmService.UpdateAccountBasedOnLeadId(
                                form.LeadId,
                                form,
                                vatFileData, form.VatCertificate?.FileName,
                                tradeLicenseData, form.TradeLicense?.FileName,
                                poaData, form.POA?.FileName,
                                Passport, form.Passport?.FileName,
                                Visa, form.Visa?.FileName,
                                EID, form.EID?.FileName,
                                accountOpeningFileData, form.AccountOpeningFile?.FileName,
                                ChequeCopy, form.ChequeCopy?.FileName,
                                EstablishmentCard, form.EstablishmentCardCopy?.FileName, false
                            );
                        }
                        else
                        {
                            // If no existing account, create a new one
                            accountUpdatedOrCreated = await _crmService.CreateAccountInCRM(
                                form,
                                vatFileData, form.VatCertificate?.FileName,
                                tradeLicenseData, form.TradeLicense?.FileName,
                                poaData, form.POA?.FileName,
                                Passport, form.Passport?.FileName,
                                Visa, form.Visa?.FileName,
                                EID, form.EID?.FileName,
                                accountOpeningFileData, form.AccountOpeningFile?.FileName,
                                ChequeCopy, form.ChequeCopy?.FileName,
                                EstablishmentCard, form.EstablishmentCardCopy?.FileName
                            );
                        }
                    }
                }
                else if (action == "Submit")
                {
                    // Logic for Submit button (create new account)
                    // Skipping Save and Submit action throws an error
                    // Code added by Sabitha on 7th May

                    //var existingAccount = await _crmService.GetAccountByLeadId(form.LeadId);
                    //if (existingAccount == null)
                    //{
                    //    accountUpdatedOrCreated = await _crmService.CreateAccountInCRM(
                    //            form,
                    //            vatFileData, form.VatCertificate?.FileName,
                    //            tradeLicenseData, form.TradeLicense?.FileName,
                    //            poaData, form.POA?.FileName,
                    //            Passport, form.Passport?.FileName,
                    //            Visa, form.Visa?.FileName,
                    //            EID, form.EID?.FileName,
                    //            accountOpeningFileData, form.AccountOpeningFile?.FileName,
                    //            ChequeCopy, form.ChequeCopy?.FileName,
                    //            EstablishmentCard, form.EstablishmentCardCopy?.FileName
                    //        );
                    //}

                    //if (!string.IsNullOrEmpty(form.LeadId)) // Assuming form has AccountId
                    //{
                    //    accountUpdatedOrCreated = await _crmService.UpdateAccountBasedOnLeadId(
                    //        form.LeadId,
                    //        form,
                    //        vatFileData, form.VatCertificate?.FileName,
                    //        tradeLicenseData, form.TradeLicense?.FileName,
                    //        poaData, form.POA?.FileName,
                    //        Passport, form.Passport?.FileName,
                    //        Visa, form.Visa?.FileName,
                    //        EID, form.EID?.FileName,
                    //        accountOpeningFileData, form.AccountOpeningFile?.FileName,
                    //        ChequeCopy, form.ChequeCopy?.FileName,
                    //        EstablishmentCard, form.EstablishmentCardCopy?.FileName,true
                    //    );
                    //}

                    // code added on aug 8

                    if (!string.IsNullOrEmpty(form.LeadId))
                    {
                        var existingAccount = await _crmService.GetAccountByLeadId(form.LeadId);

                        if (existingAccount == null)
                        {
                            // Create the account
                            accountUpdatedOrCreated = await _crmService.CreateAccountInCRM(
                                 form,
                                 vatFileData, form.VatCertificate?.FileName,
                                 tradeLicenseData, form.TradeLicense?.FileName,
                                 poaData, form.POA?.FileName,
                                 Passport, form.Passport?.FileName,
                                 Visa, form.Visa?.FileName,
                                 EID, form.EID?.FileName,
                                 accountOpeningFileData, form.AccountOpeningFile?.FileName,
                                 ChequeCopy, form.ChequeCopy?.FileName,
                                 EstablishmentCard, form.EstablishmentCardCopy?.FileName
                             );

                            // Wait or retry until account is available
                            int retryCount = 0;
                            const int maxRetries = 10;
                            while (retryCount < maxRetries)
                            {
                                await Task.Delay(1000); // wait 1 second
                                existingAccount = await _crmService.GetAccountByLeadId(form.LeadId);
                                if (existingAccount != null)
                                    break;

                                retryCount++;
                            }

                            if (existingAccount == null)
                                throw new Exception("Account was created but could not be retrieved. Try again later.");
                        }

                        // Proceed with update
                        accountUpdatedOrCreated = await _crmService.UpdateAccountBasedOnLeadId(
                           form.LeadId,
                           form,
                           vatFileData, form.VatCertificate?.FileName,
                           tradeLicenseData, form.TradeLicense?.FileName,
                           poaData, form.POA?.FileName,
                           Passport, form.Passport?.FileName,
                           Visa, form.Visa?.FileName,
                           EID, form.EID?.FileName,
                           accountOpeningFileData, form.AccountOpeningFile?.FileName,
                           ChequeCopy, form.ChequeCopy?.FileName,
                           EstablishmentCard, form.EstablishmentCardCopy?.FileName, true
                       );
                    }
                }

                // After account creation or update
                if (accountUpdatedOrCreated && action == "Submit")
                {
                    Session["SubmittedForm"] = form;
                    return RedirectToAction("Thankyou");
                }
                if (accountUpdatedOrCreated && action == "Save")
                {
                    Session["SubmittedForm"] = form;
                    return RedirectToAction("ThankyouOnSave");
                }
            }
            else
            {
                // If ModelState is not valid, log the errors
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        var fieldName = state.Key;
                        var errors = state.Value.Errors;

                        foreach (var error in errors)
                        {
                            // Log or inspect each error
                            System.Diagnostics.Trace.WriteLine($"Field: {fieldName}, Error: {error.ErrorMessage}");
                        }
                    }
                }
            }

            return View(form);
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
        [HttpPost]
        public ActionResult GeneratePdfBeforeSubmit(FormCollection form)
        {
            string templatePath = Server.MapPath("~/App_Data/Templates/Account Credit Form 1.0.docx");
            if (!System.IO.File.Exists(templatePath))
            {
                return new HttpStatusCodeResult(404, "Template not found.");
            }

            byte[] bytes = Chef_Middle_East_Form.Properties.Resources.Aspose_Words_NET;
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                // Set license for Aspose.Cells
                Aspose.Words.License lic = new Aspose.Words.License();
                lic.SetLicense(stream);
                // Now you have a stream
                // You can use the stream here
            }
            Aspose.Words.Document doc = new Aspose.Words.Document(templatePath);


            string businessCategory =
                $"{form["StatisticGroup"]}\n" +
                $"{form["ChefSegment"]}\n" +
                $"{form["SubSegment"]}";


            string RemoveKnownCountryCodes(string phoneNumber)
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                    return string.Empty;

                phoneNumber = phoneNumber.Trim();

                var countryCodes = new[] { "+971", "+968", "+974" };

                foreach (var code in countryCodes)
                {
                    if (phoneNumber.StartsWith(code))
                    {
                        return phoneNumber.Substring(code.Length).TrimStart(); // removes country code and space
                    }
                }

                return phoneNumber; // if no known code matched
            }
            string phone = string.Join("\n", new[]
            {
           RemoveKnownCountryCodes(form["PersonInChargePhoneNumber"]),
           RemoveKnownCountryCodes(form["CompanyOwnerPhoneNumber"]),
           RemoveKnownCountryCodes(form["PurchasingPersonPhoneNumber"])
       }.Where(x => !string.IsNullOrWhiteSpace(x))); // ensures blank ones are skipped

            Trace.WriteLine("Cleaned Phone:\n" + phone);



            Dictionary<string, string> formValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
            var roleMapping = new Dictionary<string, string>
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
            // ✅ Person in Charge details
            string personInChargeFullName = $"{form["PersonInChargeFirstName"]} {form["PersonInChargeLastName"]}".Trim();
            if (!string.IsNullOrWhiteSpace(personInChargeFullName))
                formValues.Add("person in charge full name", personInChargeFullName);

            if (!string.IsNullOrWhiteSpace(form["PersonInChargeEmailID"]))
                formValues.Add("person in charge contact email", form["PersonInChargeEmailID"]);

            if (!string.IsNullOrWhiteSpace(form["PersonInChargePhoneNumber"]))
                formValues.Add("person in charge mobile phone", form["PersonInChargePhoneNumber"]);

            string personInChargeRole = form["PersonInChargeRole"];
            if (roleMapping.ContainsKey(personInChargeRole))
                personInChargeRole = roleMapping[personInChargeRole]; // Convert numeric value to text

            if (!string.IsNullOrWhiteSpace(personInChargeRole))
                formValues.Add("person in charge role", personInChargeRole);

            // ✅ Purchasing Person (Use Person in Charge if missing)
            string purchasingPersonFullName = $"{form["PurchasingPersonFirstName"]} {form["PurchasingPersonLastName"]}".Trim();
            if (string.IsNullOrWhiteSpace(purchasingPersonFullName))
                purchasingPersonFullName = personInChargeFullName; // Default to Person in Charge

            if (!string.IsNullOrWhiteSpace(purchasingPersonFullName))
                formValues.Add("purchasing person full name", purchasingPersonFullName);

            string purchasingPersonEmail = form["PurchasingPersonEmailID"];
            if (string.IsNullOrWhiteSpace(purchasingPersonEmail))
                purchasingPersonEmail = form["PersonInChargeEmailID"]; // Default to Person in Charge Email

            if (!string.IsNullOrWhiteSpace(purchasingPersonEmail))
                formValues.Add("purchasing person contact email", purchasingPersonEmail);

            string purchasingPersonPhone = RemoveKnownCountryCodes(form["PurchasingPersonPhoneNumber"]);
            if (string.IsNullOrWhiteSpace(purchasingPersonPhone))
                purchasingPersonPhone = form["PersonInChargePhoneNumber"]; // Default to Person in Charge Phone

            if (!string.IsNullOrWhiteSpace(purchasingPersonPhone))
                formValues.Add("purchasing person mobile phone", purchasingPersonPhone);

            // ✅ Purchasing Person Role (Use Person in Charge Role if missing)
            string purchasingPersonRole = form["PurchasingPersonRole"];
            if (string.IsNullOrWhiteSpace(purchasingPersonRole))
                purchasingPersonRole = personInChargeRole; // Default to Person in Charge Role

            if (roleMapping.ContainsKey(purchasingPersonRole))
                purchasingPersonRole = roleMapping[purchasingPersonRole];

            if (!string.IsNullOrWhiteSpace(purchasingPersonRole))
                formValues.Add("purchasing person role", purchasingPersonRole);

            // ✅ Company Owner (Use Person in Charge if missing)
            string companyOwnerFullName = $"{form["CompanyOwnerFirstName"]} {form["CompanyOwnerLastName"]}".Trim();
            if (string.IsNullOrWhiteSpace(companyOwnerFullName))
                companyOwnerFullName = personInChargeFullName; // Default to Person in Charge

            if (!string.IsNullOrWhiteSpace(companyOwnerFullName))
                formValues.Add("company owner full name", companyOwnerFullName);

            string companyOwnerEmail = form["CompanyOwnerEmailID"];
            if (string.IsNullOrWhiteSpace(companyOwnerEmail))
                companyOwnerEmail = form["PersonInChargeEmailID"]; // Default to Person in Charge Email

            if (!string.IsNullOrWhiteSpace(companyOwnerEmail))
                formValues.Add("company owner contact email", companyOwnerEmail);

            string companyOwnerPhone = RemoveKnownCountryCodes(form["CompanyOwnerPhoneNumber"]);
            if (string.IsNullOrWhiteSpace(companyOwnerPhone))
                companyOwnerPhone = form["PersonInChargePhoneNumber"]; // Default to Person in Charge Phone

            if (!string.IsNullOrWhiteSpace(companyOwnerPhone))
                formValues.Add("company owner mobile phone", companyOwnerPhone);

            // ✅ Company Owner Role (Use Person in Charge Role if missing)
            string companyOwnerRole = form["CompanyOwnerRole"];
            if (string.IsNullOrWhiteSpace(companyOwnerRole))
                companyOwnerRole = personInChargeRole; // Default to Person in Charge Role

            if (roleMapping.ContainsKey(companyOwnerRole))
                companyOwnerRole = roleMapping[companyOwnerRole];

            if (!string.IsNullOrWhiteSpace(companyOwnerRole))
                formValues.Add("company owner role", companyOwnerRole);

            bool valuesUpdated = false;
            bool contactRowsInserted = false;
            Dictionary<string, string> attachments = new Dictionary<string, string>
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

            // ✅ Get list of available attachments
            List<string> availableAttachments = attachments
                .Where(a => Request.Files[a.Value] != null && Request.Files[a.Value].ContentLength > 0)
                .Select(a => "• " + a.Key) // Format as bullet points
                .ToList();

            if (availableAttachments.Any())
            {
                string attachmentText = string.Join("\n", availableAttachments);
                Trace.WriteLine("Attachments to insert:\n" + attachmentText);

                bool inserted = false;

                foreach (Aspose.Words.Paragraph para in doc.GetChildNodes(NodeType.Paragraph, true))
                {
                    string paraText = para.GetText().Trim().ToLower();

                    // ✅ Find the "List of Attachments" paragraph (make sure it's not empty)
                    if (paraText.Contains("list of attachments"))
                    {
                        Trace.WriteLine("✅ Found 'List of Attachments' paragraph.");

                        // ✅ Insert attachments **as a new paragraph** below the heading
                        Aspose.Words.Paragraph newParagraph = new Aspose.Words.Paragraph(doc);
                        newParagraph.AppendChild(new Run(doc, attachmentText));

                        para.ParentNode.InsertAfter(newParagraph, para);
                        inserted = true;
                        valuesUpdated = true;
                        break;
                    }
                }

                if (!inserted)
                {
                    Trace.WriteLine("⚠️ 'List of Attachments' heading was NOT found in the document.");
                }
            }
            else
            {
                Trace.WriteLine("⚠️ No valid attachments found.");
            }


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
                        .Replace("", "")
                        .Replace(":", "")
                        .ToLower();
                    Trace.WriteLine("cellText: " + cellText);
                    int valueColumnIndex = (row.Cells.Count == 2) ? 1 : row.Cells.Count - 1;

                    if (cellText.Contains("business category"))
                    {
                        row.Cells[valueColumnIndex].RemoveAllChildren();
                        string[] categories = businessCategory.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string line in categories)
                        {
                            var para = new Aspose.Words.Paragraph(doc);
                            para.AppendChild(new Run(doc, line.Trim()));
                            row.Cells[valueColumnIndex].AppendChild(para);
                        }
                        valuesUpdated = true;
                    }
                    else if (cellText == "name")
                    {
                        string parentTableText = table.Range.Text.ToLower();
                        if (parentTableText.Contains("registered address") || parentTableText.Contains("corporate address") || parentTableText.Contains("delivery address"))
                        {
                            row.Cells[valueColumnIndex].RemoveAllChildren();
                            var para = new Aspose.Words.Paragraph(doc);
                            para.AppendChild(new Run(doc, form["CompanyName"]));
                            row.Cells[valueColumnIndex].AppendChild(para);
                            valuesUpdated = true;
                        }
                    }
                    else if (formValues.ContainsKey(cellText))
                    {
                        string replacement = formValues[cellText];
                        if (!string.IsNullOrWhiteSpace(replacement))
                        {
                            row.Cells[valueColumnIndex].RemoveAllChildren();
                            var para = new Aspose.Words.Paragraph(doc);
                            para.AppendChild(new Run(doc, replacement));
                            row.Cells[valueColumnIndex].AppendChild(para);
                            valuesUpdated = true;
                        }
                    }
                    else if (cellText == "country" || cellText == "city" || cellText == "street")
                    {
                        string section = GetAddressSection(row);
                        string valueToSet = null;

                        if (section == "registered")
                        {
                            if (cellText == "country")
                                valueToSet = !string.IsNullOrWhiteSpace(form["RegisteredCountry"]) ? form["RegisteredCountry"] : form["DeliveryCountry"];
                            else if (cellText == "city")
                                valueToSet = !string.IsNullOrWhiteSpace(form["RegisteredCity"]) ? form["RegisteredCity"] : form["DeliveryCity"];
                            else if (cellText == "street")
                                valueToSet = !string.IsNullOrWhiteSpace(form["RegisteredStreet"]) ? form["RegisteredStreet"] : form["DeliveryStreet"];
                        }

                        else if (section == "corporate")
                        {
                            if (cellText == "country") valueToSet = form["CorporateCountry"];
                            else if (cellText == "city") valueToSet = form["CorporateCity"];
                            else if (cellText == "street") valueToSet = form["CorporateStreet"];
                        }
                        else if (section == "delivery")
                        {
                            if (cellText == "country") valueToSet = form["DeliveryCountry"];
                            else if (cellText == "city") valueToSet = form["DeliveryCity"];
                            else if (cellText == "street") valueToSet = form["DeliveryStreet"];
                        }

                        if (!string.IsNullOrWhiteSpace(valueToSet))
                        {
                            if (row.Cells.Count >= 2)
                            {
                                // Normal two-column row
                                row.Cells[valueColumnIndex].RemoveAllChildren();
                                var para = new Aspose.Words.Paragraph(doc);
                                para.AppendChild(new Run(doc, valueToSet));
                                row.Cells[valueColumnIndex].AppendChild(para);
                                valuesUpdated = true;
                            }
                            else
                            {
                                // Possibly a single-cell row: update the first cell of the next row
                                Table parenttable = (Table)row.GetAncestor(NodeType.Table);
                                int currentIndex = table.Rows.IndexOf(row);
                                if (currentIndex + 1 < table.Rows.Count)
                                {
                                    Row nextRow = table.Rows[currentIndex + 1];
                                    if (nextRow.Cells.Count > 0)
                                    {
                                        nextRow.Cells[0].RemoveAllChildren();
                                        var para = new Aspose.Words.Paragraph(doc);
                                        para.AppendChild(new Run(doc, valueToSet));
                                        nextRow.Cells[0].AppendChild(para);
                                        valuesUpdated = true;
                                    }
                                }
                            }
                        }
                    }

                }
            }

            // Hide watermark if exists
            foreach (Aspose.Words.Paragraph para in doc.GetChildNodes(NodeType.Paragraph, true))
            {
                string text = para.GetText().Trim();
                if (text.Contains("Evaluation Only") || text.Contains("Created with an evaluation copy of Aspose.Words"))
                {
                    foreach (Run run in para.Runs)
                    {
                        run.Font.Color = System.Drawing.Color.White; // Hide watermark
                    }
                }
            }

            // Only generate and return PDF if values were updated or contact rows were inserted
            if (valuesUpdated || contactRowsInserted)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    doc.Save(stream, SaveFormat.Pdf);
                    stream.Position = 0;
                    return File(stream.ToArray(), "application/pdf", "Account_Opening_Form_Preview.pdf");
                }
            }

            return new HttpStatusCodeResult(500, "Failed to generate PDF. No values were updated.");
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
