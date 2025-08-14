using Chef_Middle_East_Form.Models;
using iText.Layout.Properties;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Deployment;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Services.Description;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;
using Form = Chef_Middle_East_Form.Models.Form;

namespace Chef_Middle_East_Form.Services
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content)
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri) { Content = content };
            return await client.SendAsync(request);
        }
    }

    public class CRMService : ICRMService
    {
        private static readonly string ClientId = ConfigurationManager.AppSettings["CRMClientId"];
        private static readonly string TenantId = ConfigurationManager.AppSettings["CRMTenantId"];
        private static readonly string ClientSecret = ConfigurationManager.AppSettings["CRMClientSecret"];
        private static readonly string CRMUrl = ConfigurationManager.AppSettings["CRMAppUrl"] + "/api/data/v9.2/accounts";
        private static readonly string CRMAppUrl = ConfigurationManager.AppSettings["CRMAppUrl"];

        private async Task<string> GetAccessToken()
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/token");
                var body = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", ClientId),
                    new KeyValuePair<string, string>("client_secret", ClientSecret),
                    new KeyValuePair<string, string>("scope", ConfigurationManager.AppSettings["CRMAppUrl"] + "/.default"),
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                });
                request.Content = body;
                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Trace.WriteLine($"Token Retrieval Failed: {response.StatusCode} - {responseBody}");
                    return null;
                }
                JObject tokenResponse = JObject.Parse(responseBody);
                return tokenResponse["access_token"]?.ToString();
            }
        }
        public async Task<Form> GetLeadData(string leadId)
        {
            var accessToken = await GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                return null;
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.Add("Prefer", "odata.include-annotations=\"OData.Community.Display.V1.FormattedValue\"");

                string leadUrl = $"{ConfigurationManager.AppSettings["CRMAppUrl"]}/api/data/v9.2/leads({leadId})?$select=companyname,mobilephone,emailaddress1,_nw_statisticgroup_value,_nw_chefsegments_value,_nw_subsegment_value,_msdyn_company_value,_shp_classification_value,cr5b1_reason,cr5b1_isecommerce,cr5b1_ifusinginventorysystem,shp_emailsenton,statuscode";

                var response = await client.GetAsync(leadUrl);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        System.Diagnostics.Trace.WriteLine("Warning: Received empty response from CRM API.");
                        return null;
                    }

                    JObject leadData;
                    try
                    {
                        leadData = JsonConvert.DeserializeObject<JObject>(content);
                        if (leadData == null)
                        {
                            System.Diagnostics.Trace.WriteLine("Warning: Lead data is null after deserialization.");
                            return null;
                        }
                    }
                    catch (JsonException ex)
                    {
                        System.Diagnostics.Trace.WriteLine($"Error parsing lead data: {ex.Message}");
                        return null;
                    }

                    return new Form
                    {
                        CompanyName = leadData["companyname"]?.ToString(),
                        MainPhone = leadData["mobilephone"]?.ToString(),
                        Email = leadData["emailaddress1"]?.ToString(),
                        StatisticGroup = leadData["_nw_statisticgroup_value@OData.Community.Display.V1.FormattedValue"]?.ToString(),
                        ChefSegment = leadData["_nw_chefsegments_value@OData.Community.Display.V1.FormattedValue"]?.ToString(),
                        SubSegment = leadData["_nw_subsegment_value@OData.Community.Display.V1.FormattedValue"]?.ToString(),
                        Branch = leadData["_msdyn_company_value@OData.Community.Display.V1.FormattedValue"]?.ToString(),
                        Classification = leadData["_shp_classification_value@OData.Community.Display.V1.FormattedValue"]?.ToString(),
                        Reason = leadData["cr5b1_reason"]?.ToString(),
                        Ecomerce = leadData["cr5b1_isecommerce"]?.ToString(),
                        InventorySystem = leadData["cr5b1_ifusinginventorysystem"]?.ToString(),
                        StatusCode = leadData["statuscode"]?.ToString(),
                        EmailSenton = leadData["shp_emailsenton"] != null ? leadData["shp_emailsenton"].ToObject<DateTime?>() ?? default : default
                    };
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine($"Error: {response.StatusCode}, Response: {content}");
                }
            }
            return null;
        }


        public async Task<JObject> GetAccountByLeadId(string leadId)
        {
            var accessToken = await GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
                return null;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.Add("Prefer", "odata.include-annotations=\"OData.Community.Display.V1.FormattedValue\"");

                string url = $"{ConfigurationManager.AppSettings["CRMAppUrl"]}/api/data/v9.2/accounts?" +
                    "$filter=_originatingleadid_value eq " + leadId + "&" +
                    "$select=" +
                        "accountid," +
                        "_msdyn_customerpaymentmethod_value," +
                        "_primarycontactid_value," +  // For GUID of primary contact
                        "_nw_owner_value," +            // For GUID of owner
                        "_nw_purchasingperson_value," + // For GUID of purchasing person
                        "_nw_country11_value," +        // For GUID of country
                        "_nw_city_value," +             // For GUID of city
                        "_nw_deliverycountry_value," +  // For GUID of delivery country
                        "_nw_deliverycity_value," +     // For GUID of delivery city
                        "_nw_countries_value," +        // For GUID of countries
                        "_nw_cityregisteredaddress_value," + // For GUID of registered address city
                        "_nw_bankaccount_value," +      // For GUID of bank account
                        "name," +
                        "emailaddress1," +
                        "telephone1," +
                        "nw_licenseexpirydate," +
                        "address1_line1," +
                        "shippingmethodcode," +
                        "address2_line1," +
                        "address2_shippingmethodcode," +
                        "nw_address3street," +
                        "nw_proposecreditlimit," +
                        "nw_proposecreditlimit1," +
                        "nw_estimatedpurchasevalue," +
                        "nw_estimatedmonthlypurchaseaed," +
                        "nw_checkcopy," +
                        "nw_issametocorporateaddress," +
                        "nw_iscontactpersonsameaspurchasing," +
                        "nw_scontactpersonsameascompanyowner," +
                        "nw_vattrnattachcertificate," +
                        "nw_accountopeningfile," +
                        "nw_powerofattorney,nw_amountofsecuritychequeamountaed," +
                        "nw_passport," +
                        "nw_visa," +
                        "nw_emiratesidcard," +
                        "nw_tradecommerciallicensenoattachlicense," +
                        "nw_tradenameoutletname," +
                        "nw_tradelicensenumber," +
                        "nw_vatnumber," +
                        "nw_establishmentcardcopy," +
                        "nw_establishmentcardnumber," +
                        "nw_proposedpaymentterms";

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = JsonConvert.DeserializeObject<JObject>(content);
                        System.Diagnostics.Trace.WriteLine(json);
                        var records = json["value"] as JArray;
                        if (records != null && records.Count > 0)
                        {
                            var accountData = records?.FirstOrDefault() as JObject;

                            if (accountData != null && accountData["accountid"] != null)
                            {
                                var lookupFields = new Dictionary<string, string>
                            {
                                { "_nw_country11_value", "nw_countries" },
                                { "_nw_city_value", "nw_cities" },
                                { "_nw_deliverycountry_value", "nw_countries" },
                                { "_nw_deliverycity_value", "nw_cities" },
                                { "_nw_countries_value", "nw_countries" },
                                { "_nw_cityregisteredaddress_value", "nw_cities" },
                            };

                                foreach (var lookupField in lookupFields)
                                {
                                    var guid = accountData[lookupField.Key]?.ToString();

                                    if (!string.IsNullOrEmpty(guid))
                                    {
                                        try
                                        {
                                            var lookupName = await GetLookupName(guid, lookupField.Value);
                                            if (!string.IsNullOrEmpty(lookupName))
                                            {
                                                // Use a unique key for each lookup field.
                                                accountData[lookupField.Key.Replace("_value", "_name")] = lookupName; // Using original key, and replacing _value with _name for new key.
                                                System.Diagnostics.Trace.WriteLine($"Updated {lookupField.Key} with: {lookupName}");
                                            }
                                            else
                                            {
                                                accountData[lookupField.Key.Replace("_value", "_name")] = null;
                                                System.Diagnostics.Trace.WriteLine($"Lookup name not found for {lookupField.Key}");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            accountData[lookupField.Key.Replace("_value", "_name")] = null;
                                            System.Diagnostics.Trace.WriteLine($"Error fetching lookup name for {lookupField.Key}: {ex.Message}");
                                        }
                                    }
                                    else
                                    {
                                        accountData[lookupField.Key.Replace("_value", "_name")] = null;
                                        System.Diagnostics.Trace.WriteLine($"GUID is null or empty for {lookupField.Key}");
                                    }
                                }

                                var primaryContactId = accountData["_primarycontactid_value"]?.ToString();
                                var ownerId = accountData["_nw_owner_value"]?.ToString();
                                var purchasingPersonId = accountData["_nw_purchasingperson_value"]?.ToString();

                                if (!string.IsNullOrEmpty(primaryContactId))
                                {
                                    try
                                    {
                                        var primaryContactDetails = await GetContactDetailsByGuid(primaryContactId);
                                        if (primaryContactDetails != null)
                                        {
                                            // Prefix properties with "primaryContact_".
                                            foreach (var property in primaryContactDetails.Properties())
                                            {
                                                accountData[$"primaryContact_{property.Name}"] = property.Value;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error fetching primary contact details: {ex.Message}");
                                    }
                                }

                                if (!string.IsNullOrEmpty(ownerId))
                                {
                                    try
                                    {
                                        var ownerDetails = await GetContactDetailsByGuid(ownerId);
                                        if (ownerDetails != null)
                                        {
                                            // Prefix properties with "owner_".
                                            foreach (var property in ownerDetails.Properties())
                                            {
                                                accountData[$"owner_{property.Name}"] = property.Value;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error fetching owner details: {ex.Message}");
                                    }
                                }

                                if (!string.IsNullOrEmpty(purchasingPersonId))
                                {
                                    try
                                    {
                                        var purchasingPersonDetails = await GetContactDetailsByGuid(purchasingPersonId);
                                        if (purchasingPersonDetails != null)
                                        {
                                            // Prefix properties with "purchasingPerson_".
                                            foreach (var property in purchasingPersonDetails.Properties())
                                            {
                                                accountData[$"purchasingPerson_{property.Name}"] = property.Value;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error fetching purchasing person details: {ex.Message}");
                                    }
                                }

                                if (!string.IsNullOrEmpty(purchasingPersonId))
                                {
                                    try
                                    {
                                        var purchasingPersonDetails = await GetContactDetailsByGuid(purchasingPersonId);
                                        if (purchasingPersonDetails != null)
                                        {
                                            foreach (var property in purchasingPersonDetails.Properties())
                                            {
                                                accountData[property.Name] = property.Value;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error fetching purchasing person details: {ex.Message}");
                                    }
                                }
                                var bankAccountId = accountData["_nw_bankaccount_value"]?.ToString();

                                if (!string.IsNullOrEmpty(bankAccountId))
                                {
                                    try
                                    {
                                        // Retrieve specific bank account parameters from the API.
                                        JObject bankAccountDetails = await GetSpecificBankAccountDetailsById(bankAccountId);

                                        if (bankAccountDetails != null)
                                        {
                                            // Add parameters to accountData.
                                            foreach (var property in bankAccountDetails.Properties())
                                            {
                                                accountData[$"bankAccount_{property.Name}"] = property.Value;
                                            }
                                        }
                                        else
                                        {
                                            System.Diagnostics.Trace.WriteLine($"Bank account details not found for ID: {bankAccountId}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Trace.WriteLine($"Error retrieving bank account details: {ex.Message}");
                                    }
                                }
                                string accountId = accountData["accountid"].ToString();

                                // Download file content for each file field
                                var uploadFolder = HttpContext.Current.Server.MapPath("~/App_Data/uploads");
                                System.Diagnostics.Trace.WriteLine(uploadFolder);
                                if (!Directory.Exists(uploadFolder))
                                {
                                    Directory.CreateDirectory(uploadFolder);  // Create the folder if it doesn't exist
                                }

                                var filePaths = new Dictionary<string, byte[]>
                            {
                                { "tradeLicense", await DownloadFileFromAccount(accountId, "nw_tradecommerciallicensenoattachlicense") },
                                { "vatCertificate", await DownloadFileFromAccount(accountId, "nw_vattrnattachcertificate") },
                                { "powerOfAttorney", await DownloadFileFromAccount(accountId, "nw_powerofattorney") },
                                { "passport", await DownloadFileFromAccount(accountId, "nw_passport") },
                                { "visa", await DownloadFileFromAccount(accountId, "nw_visa") },
                                { "emiratesId", await DownloadFileFromAccount(accountId, "nw_emiratesidcard") },
                                { "checkCopy", await DownloadFileFromAccount(accountId, "nw_checkcopy") },
                                { "accountOpeningFile", await DownloadFileFromAccount(accountId, "nw_accountopeningfile") },
                                { "EstablishmentCardCopy", await DownloadFileFromAccount(accountId, "nw_establishmentcardcopy") }
                            };
                                foreach (var file in filePaths)
                                {
                                    if (file.Value != null)
                                    {
                                        // Use the dictionary key as the base file name.
                                        string baseFileName = file.Key;

                                        string fileExtension = ".pdf"; // Default to .pdf if unknown.

                                        string fileName = baseFileName + "_" + accountId + fileExtension;
                                        var filePath = Path.Combine(uploadFolder, fileName);

                                        try
                                        {
                                            File.WriteAllBytes(filePath, file.Value);
                                            accountData[$"{file.Key}_filePath"] = "~/Uploads/" + fileName; // Relative path.
                                        }
                                        catch (IOException ex)
                                        {
                                            // Handle file write error.
                                            Console.WriteLine($"Error writing file {fileName}: {ex.Message}");
                                            accountData[$"{file.Key}_filePath"] = "File Write Error";
                                        }
                                    }
                                    else
                                    {
                                        accountData[$"{file.Key}_filePath"] = "No File Available";
                                    }
                                }

                            }
                            System.Diagnostics.Trace.WriteLine(accountData);
                            return accountData;
                        }
                        else
                        {
                            System.Diagnostics.Trace.WriteLine("Warning: No account records found for the given Lead ID.");
                        }
                    }
                }

                else
                {
                    Trace.WriteLine(response + content);
                }
                return null;
            }
        }
        private async Task<JObject> GetContactDetailsByGuid(string contactId)
        {
            var accessToken = await GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
                return null;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                string contactUrl = $"{CRMAppUrl}/api/data/v9.2/contacts({contactId})?$select=firstname,lastname,emailaddress1,mobilephone,nw_rule";
                var response = await client.GetAsync(contactUrl);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var contactData = JsonConvert.DeserializeObject<JObject>(content);
                    System.Diagnostics.Trace.WriteLine(contactData);
                    return contactData;
                }

                return null;
            }
        }
        private async Task<string> GetLookupName(string guid, string entityName)
        {
            // Set the appropriate field to retrieve based on the entity name
            string fieldToSelect = entityName == "nw_countries" ? "nw_description" : "nw_name";

            var url = $"{CRMAppUrl}/api/data/v9.2/{entityName}({guid})?$select={fieldToSelect}";
            var accessToken = await GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
                return null;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<JObject>(content);

                // Return the appropriate field based on the entity
                return jsonResponse[fieldToSelect]?.ToString();
            }

            return null; // If no name is found or request fails
        }

        public async Task<JObject> GetUploadedFileName(string accountId, string attachmentId)
        {
            var accessToken = await GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                System.Diagnostics.Trace.WriteLine("Access token is missing.");
                return null;
            }
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Construct the file URL for the specific field (file).
                string fileUrl = $"{ConfigurationManager.AppSettings["CRMAppUrl"]}/api/data/v9.2/fileattachments({attachmentId})?$select=filename";

                try
                {
                    // Send the GET request
                    var response = await client.GetAsync(fileUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        // Read and return the file content as a byte array.
                        string content = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<JObject>(content);
                    }
                    else
                    {
                        // Log failure and response details
                        System.Diagnostics.Trace.WriteLine($"Failed to download file. Status Code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                        var errorDetails = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Trace.WriteLine($"Error Details: {errorDetails}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception and URL
                    System.Diagnostics.Trace.WriteLine($"Error occurred while downloading file from {fileUrl}: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<byte[]> DownloadFileFromAccount(string accountId, string fieldName)
        {
            // Get access token
            var accessToken = await GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                System.Diagnostics.Trace.WriteLine("Access token is missing.");
                return null;
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Construct the file URL for the specific field (file).
                string fileUrl = $"{ConfigurationManager.AppSettings["CRMAppUrl"]}/api/data/v9.2/accounts({accountId})/{fieldName}/$value";

                try
                {
                    // Log the file URL for debugging purposes.
                    System.Diagnostics.Trace.WriteLine($"Downloading file from: {fileUrl}");

                    // Send the GET request
                    var response = await client.GetAsync(fileUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        // Read and return the file content as a byte array.
                        return await response.Content.ReadAsByteArrayAsync();
                    }
                    else
                    {
                        // Log failure and response details
                        System.Diagnostics.Trace.WriteLine($"Failed to download file. Status Code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                        var errorDetails = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Trace.WriteLine($"Error Details: {errorDetails}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception and URL
                    System.Diagnostics.Trace.WriteLine($"Error occurred while downloading file from {fileUrl}: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<JObject> GetSpecificBankAccountDetailsById(string bankAccountId)
        {
            using (var client = new HttpClient())
            {
                // Add Authorization header with access token.
                var accessToken = await GetAccessToken(); // Replace with your token retrieval method.
                if (string.IsNullOrEmpty(accessToken))
                {
                    System.Diagnostics.Trace.WriteLine("Access Token was null or empty");
                    return null;
                }
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                try
                {
                    // Construct the API URL with $select.
                    string selectParameters = "nw_name,nw_bank,nw_bankname,nw_bankaddress,nw_swiftcode,nw_ibannumber";
                    string apiUrl = $"{CRMAppUrl}/api/data/v9.2/nw_bankaccounts({bankAccountId})?$select={selectParameters}";

                    var response = await client.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<JObject>(content);
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine($"HTTP request failed. Status code: {response.StatusCode}");
                        return null;
                    }
                }
                catch (HttpRequestException ex)
                {
                    System.Diagnostics.Trace.WriteLine($"HTTP request error: {ex.Message}");
                    return null;
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Trace.WriteLine($"JSON parsing error: {ex.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"An unexpected error occurred: {ex.Message}");
                    return null;
                }
            }
        }

        private async Task<string> GetLookupFieldIdByName(HttpClient client, string entitySetName, string fieldName, string filtercolumn, string value,string companyID)
        {
            var sanitizedValue = value.Replace("'", "''");
            var queryUrl = $"{ConfigurationManager.AppSettings["CRMAppUrl"]}/api/data/v9.2/{entitySetName}?$select={fieldName}id&$filter={filtercolumn} eq '{sanitizedValue}' and _nw_company_value eq '{companyID}'";
            var response = await client.GetAsync(queryUrl);
            
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                dynamic result = JsonConvert.DeserializeObject(content);
                if (result.value.Count > 0)
                {
                    var id = result.value[0][$"{fieldName}id"];
                    System.Diagnostics.Trace.WriteLine($"Found ID for {entitySetName}: {id}");
                    return result.value[0][$"{fieldName}id"];
                }
            }
            return null;
        }
        private async Task<string> GetLookupField(HttpClient client, string entitySetName, string fieldName, string filtercolumn, string value)
        {
            if (value == null)
                return null;
            var sanitizedValue = value.Replace("'", "''");
            var queryUrl = $"{ConfigurationManager.AppSettings["CRMAppUrl"]}/api/data/v9.2/{entitySetName}?$select={fieldName}id&$filter={filtercolumn} eq '{sanitizedValue}'";
            var response = await client.GetAsync(queryUrl);

            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                dynamic result = JsonConvert.DeserializeObject(content);
                if (result.value.Count > 0)
                {
                    var id = result.value[0][$"{fieldName}id"];
                    System.Diagnostics.Trace.WriteLine($"Found ID for {entitySetName}: {id}");
                    return result.value[0][$"{fieldName}id"];
                }
            }
            else
            {
                System.Diagnostics.Trace.WriteLine(response);
            }
            return null;
        }

        private async Task<string> GetPaymentMethodIdByName(HttpClient client, string paymentMethodName,string companyId)
        {
            var sanitizedValue = paymentMethodName.Replace("'", "''"); // Hardcoded GUID as per requirement

            var queryUrl = $"{ConfigurationManager.AppSettings["CRMAppUrl"]}/api/data/v9.2/msdyn_customerpaymentmethods" +
                           $"?$select=msdyn_customerpaymentmethodid" +
                           $"&$filter=msdyn_name eq '{sanitizedValue}' and _msdyn_company_value eq {companyId}";

            System.Diagnostics.Trace.WriteLine($"Payment Method Lookup URL: {queryUrl}");

            var response = await client.GetAsync(queryUrl);
            var content = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Trace.WriteLine($"Payment Method Response: {content}");

            if (response.IsSuccessStatusCode)
            {
                dynamic result = JsonConvert.DeserializeObject(content);
                if (result.value.Count > 0)
                {
                    var id = result.value[0]["msdyn_customerpaymentmethodid"];
                    System.Diagnostics.Trace.WriteLine($"Found Payment Method ID: {id}");
                    return id;
                }
            }

            System.Diagnostics.Trace.WriteLine($"Payment method '{paymentMethodName}' not found with the specified company filter.");
            return null;
        }
        public async Task<bool> CreateAccountInCRM(
            Form model,
            byte[] vatFileData, string vatFileName,
            byte[] tradeLicenseData, string tradeLicenseName,
            byte[] poaData, string poaFileName,
            byte[] Passport, string PassportFileName,
            byte[] Visa, string VisaFileName,
            byte[] EID, string EIDFileName,
            byte[] accountOpeningFileData, string accountOpeningFileName,
            byte[] chequefiledata, string chequefilename,
            byte[] EstablishmentCardFileData, string EstablishmentCardFileName)
        {
            var accessToken = await GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                return false;
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.Add("OData-Version", "4.0");
                client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");

                var BranchID = await GetLookupField(client, "cdm_companies", "cdm_company", "cdm_companycode", model.Branch);
                var paymentMethodId = await GetPaymentMethodIdByName(client, model.CustomerPaymentMethod.ToUpper(), BranchID);
                var statisticGroupId = await GetLookupFieldIdByName(client, "nw_statisticgroups", "nw_statisticgroup", "nw_name", model.StatisticGroup, BranchID);
                var chefSegmentId = await GetLookupFieldIdByName(client, "nw_chefsegments", "nw_chefsegment", "nw_name", model.ChefSegment, BranchID);
                var subSegmentId = await GetLookupFieldIdByName(client, "nw_subsegments", "nw_subsegment", "nw_name", model.SubSegment, BranchID);
                var ClassificationID = await GetLookupFieldIdByName(client, "nw_classifications", "nw_classification", "nw_name", model.Classification, BranchID);
                var CorporateCountryID = await GetLookupField(client, "nw_countries", "nw_country", "nw_description", model.CorporateCountry);
                var CorporateCityID = await GetLookupField(client, "nw_cities", "nw_city", "nw_name", model.CorporateCity);
                string DeliveryCountryID = null;
                string DeliveryCityID = null;
                string RegisteredCountryID = null;
                string RegisteredCityID = null;
                if (!string.IsNullOrWhiteSpace(model.DeliveryCountry))
                    DeliveryCountryID = await GetLookupField(client, "nw_countries", "nw_country", "nw_description", model.DeliveryCountry);

                if (!string.IsNullOrWhiteSpace(model.DeliveryCity))
                    DeliveryCityID = await GetLookupField(client, "nw_cities", "nw_city", "nw_name", model.DeliveryCity);

                if (!string.IsNullOrWhiteSpace(model.RegisteredCountry))
                    RegisteredCountryID = await GetLookupField(client, "nw_countries", "nw_country", "nw_description", model.RegisteredCountry);

                if (!string.IsNullOrWhiteSpace(model.RegisteredCity))
                    RegisteredCityID = await GetLookupField(client, "nw_cities", "nw_city", "nw_name", model.RegisteredCity);
                //var DeliveryCountryID = await GetLookupField(client, "nw_countries", "nw_country", "nw_description", model.DeliveryCountry);
                //var DeliveryCityID = await GetLookupField(client, "nw_cities", "nw_city", "nw_name", model.DeliveryCity);
                //var RegisteredCountryID = await GetLookupField(client, "nw_countries", "nw_country", "nw_description", model.RegisteredCountry);
                //var RegisteredCityID = await GetLookupField(client, "nw_cities", "nw_city", "nw_name", model.RegisteredCity);

                JObject jsonAccount = new JObject
                {
                    ["name"] = model.CompanyName,
                    ["telephone1"] = model.MainPhone,
                    ["emailaddress1"] = model.Email,
                    ["msdyn_customerpaymentmethod@odata.bind"] = $"/msdyn_customerpaymentmethods({paymentMethodId})",
                    ["msdyn_company@odata.bind"] = $"/cdm_companies({BranchID})",
                    ["nw_StatisticGroup@odata.bind"] = $"/nw_statisticgroups({statisticGroupId})",
                    ["nw_ChefSegment@odata.bind"] = $"/nw_chefsegments({chefSegmentId})",
                    ["nw_SubSegment@odata.bind"] = $"/nw_subsegments({subSegmentId})",
                    ["nw_Classification1@odata.bind"] = $"/nw_classifications({ClassificationID})",
                    ["paymenttermscode"] = 6,
                    ["nw_tradenameoutletname"] = model.TradeName,
                    ["nw_tradelicensenumber"] = model.TradeLicenseNumber,
                    ["nw_licenseexpirydate"] = model.LicenseExpiryDate,
                    ["nw_vatnumber"] = model.VatNumber,
                    ["nw_iscontactpersonsameaspurchasing"] = model.IsContactPersonSameAsPurchasing == "1" ? true : false,
                    ["nw_scontactpersonsameascompanyowner"] = model.IsContactPersonSameAsCompanyOwner == "1" ? true : false,
                    ["nw_issametocorporateaddress"] = model.IsSameAsCorporateAddress == "1" ? true : false,
                    ["address1_line1"] = model.CorporateStreet,
                    ["shippingmethodcode"] = int.Parse(model.CorporateShippingMethod),
                    ["address2_line1"] = model.DeliveryStreet,
                    ["address2_shippingmethodcode"] = int.Parse(model.DeliveryShippingMethod),
                    ["nw_address3street"] = model.RegisteredStreet,
                    ["nw_estimatedpurchasevalue"] = model.EstimatedPurchaseValue,
                    ["nw_estimatedmonthlypurchaseaed"] = model.EstimatedMonthlyPurchase,
                    ["nw_amountofsecuritychequeamountaed"] = model.SecurityChequeAmount,
                    ["nw_proposecreditlimit"] = model.CreditLimit,
                    ["nw_proposecreditlimit1"] = model.RequestedCreditLimit,
                    ["new_isecommerce"] = model.Ecomerce,
                    ["new_reason"] = model.Reason,
                    ["new_ifusinginventorysystem"] = model.InventorySystem,
                    ["nw_establishmentcardnumber"] = model.EstablishmentCardNumber,



                };
                if (!string.IsNullOrWhiteSpace(model.ProposedPaymentTerms))
                {
                    jsonAccount["nw_proposeanotherterm"] = true;
                    jsonAccount["nw_proposedpaymentterms"] = model.ProposedPaymentTerms;
                }

                if (!string.IsNullOrWhiteSpace(DeliveryCountryID))
                {
                    jsonAccount["nw_DeliveryCountry@odata.bind"] = $"/nw_countries({DeliveryCountryID})";
                }
                if (!string.IsNullOrWhiteSpace(DeliveryCityID))
                {
                    jsonAccount["nw_DeliveryCity@odata.bind"] = $"/nw_cities({DeliveryCityID})";
                }

                if (!string.IsNullOrWhiteSpace(CorporateCountryID))
                {
                    jsonAccount["nw_Country11@odata.bind"] = $"/nw_countries({CorporateCountryID})";
                }

                if (!string.IsNullOrWhiteSpace(CorporateCityID))
                {
                    jsonAccount["nw_City@odata.bind"] = $"/nw_cities({CorporateCityID})";
                }
                if (!string.IsNullOrEmpty(model.LeadId))
                {
                    jsonAccount["originatingleadid@odata.bind"] = $"/leads/{model.LeadId}";
                }
                if (!string.IsNullOrEmpty(RegisteredCountryID))
                    jsonAccount["nw_Countries@odata.bind"] = $"/nw_countries({RegisteredCountryID})";

                if (!string.IsNullOrEmpty(RegisteredCityID))
                    jsonAccount["nw_Cityregisteredaddress@odata.bind"] = $"/nw_cities({RegisteredCityID})";

                var payload = JsonConvert.SerializeObject(jsonAccount);
                System.Diagnostics.Trace.WriteLine($"Payload: {payload}");

                if (string.IsNullOrEmpty(paymentMethodId) || string.IsNullOrEmpty(statisticGroupId) ||
                    string.IsNullOrEmpty(chefSegmentId) || string.IsNullOrEmpty(subSegmentId))
                {
                    throw new Exception("One or more required lookup fields were not found. Payload Generated : " + payload);
                }
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(CRMUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Trace.WriteLine($"Response Status Code: {response.StatusCode}");
                System.Diagnostics.Trace.WriteLine($"Raw Response Content: {responseContent ?? "No content returned"}");

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Trace.WriteLine($"Response Status Code: {response.StatusCode}");
                    System.Diagnostics.Trace.WriteLine($"Error Content: {responseContent}");
                    throw new Exception($"CRM Create Account Failed: {responseContent}  Payload:  {payload}");
                }

                string accountId = null;
                if (response.Headers.Location != null)
                {
                    string locationUrl = response.Headers.Location.AbsoluteUri;
                    System.Diagnostics.Trace.WriteLine($"Location URL: {locationUrl}");

                    if (locationUrl.Contains("(") && locationUrl.Contains(")"))
                    {
                        accountId = locationUrl.Split('(')[1].Split(')')[0];
                    }
                }

                System.Diagnostics.Trace.WriteLine($"Extracted Account ID: {accountId}");

                // Step 2: Attach Files if provided
                if (!string.IsNullOrEmpty(accountId))
                {
                    Guid recordGuid = new Guid(accountId); // Convert accountId to GUID
                    string entityName = "account"; // Ensure this matches your entity name

                    // Dictionary containing file fields and their corresponding data
                    Dictionary<string, (byte[] fileData, string fileName)> fileFields = new Dictionary<string, (byte[], string)>
                    {
                        { "nw_vattrnattachcertificate", (vatFileData, vatFileName) },  // VAT Certificate
                        { "nw_tradecommerciallicensenoattachlicense", (tradeLicenseData, tradeLicenseName) },  // Trade License
                        { "nw_accountopeningfile", (accountOpeningFileData, accountOpeningFileName) },  // Account Opening File
                        { "nw_powerofattorney",(poaData,poaFileName) },
                        { "nw_passport" ,(Passport,PassportFileName)},
                        { "nw_visa" ,(Visa,VisaFileName)},
                        { "nw_emiratesidcard" ,(EID,EIDFileName)},
                        { "nw_checkcopy" ,(chequefiledata,chequefilename)},
                        {"nw_establishmentcardcopy",(EstablishmentCardFileData,EstablishmentCardFileName)},
                        
                    };

                    foreach (var fileEntry in fileFields)
                    {
                        string fileAttribute = fileEntry.Key;
                        byte[] fileData = fileEntry.Value.fileData;
                        string fileName = fileEntry.Value.fileName;

                        if (fileData != null && !string.IsNullOrEmpty(fileName))
                        {
                            try
                            {
                                await UploadFileToCRM(recordGuid, entityName, fileAttribute, fileData, fileName);
                                System.Diagnostics.Trace.WriteLine($"✅ {fileName} successfully uploaded to '{fileAttribute}'.");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine($"⚠ Failed to attach {fileName}: {ex.Message}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Trace.WriteLine($"⚠ Skipping upload for '{fileAttribute}' as file data is missing.");
                        }
                    }
                }
                if (!string.IsNullOrEmpty(accountId))
                {
                    if (!string.IsNullOrWhiteSpace(model.IBNAccountNumber) ||
                         !string.IsNullOrWhiteSpace(model.Bank) ||
                         !string.IsNullOrWhiteSpace(model.BankName) ||
                         !string.IsNullOrWhiteSpace(model.BankAddress) ||
                         !string.IsNullOrWhiteSpace(model.SwiftCode) ||
                         !string.IsNullOrWhiteSpace(model.IbanNumber))
                    {
                        var bankAccountJson = new JObject
                        {
                            ["nw_name"] = model.IBNAccountNumber,
                            ["nw_bank"] = model.Bank,
                            ["nw_bankname"] = model.BankName,
                            ["nw_bankaddress"] = model.BankAddress,
                            ["nw_swiftcode"] = model.SwiftCode,
                            ["nw_ibannumber"] = model.IbanNumber,
                            ["nw_Accountid@odata.bind"] = $"/accounts({accountId})"
                        };

                        System.Diagnostics.Trace.WriteLine(bankAccountJson);

                        var bankAccountResponse = await client.PostAsync(
                            CRMAppUrl+"/api/data/v9.2/nw_bankaccounts",
                            new StringContent(bankAccountJson.ToString(), Encoding.UTF8, "application/json")
                        );

                        System.Diagnostics.Trace.WriteLine(bankAccountResponse);

                        if (bankAccountResponse.IsSuccessStatusCode && bankAccountResponse.Headers.Location != null)
                        {
                            var bankAccountUri = bankAccountResponse.Headers.Location;
                            var lastSegment = bankAccountUri.Segments.Last().Trim('/');
                            var match = Regex.Match(lastSegment, @"\(([^)]+)\)");
                            var bankAccountId = match.Success ? match.Groups[1].Value : lastSegment;

                            // Step 2: Update Account with Bank Account lookup
                            var updateAccountWithBank = new JObject
                            {
                                ["nw_BankAccount@odata.bind"] = $"/nw_bankaccounts({bankAccountId})"
                            };

                            await client.PatchAsync($"{CRMUrl}({accountId})", new StringContent(updateAccountWithBank.ToString(), Encoding.UTF8, "application/json"));
                        }
                        else
                        {
                            var error = await bankAccountResponse.Content.ReadAsStringAsync();
                            Trace.WriteLine("⚠️ Failed to create bank account: " + error);
                        }
                    }
                    else
                    {
                        Trace.WriteLine("ℹ️ Skipping bank account creation – no bank details provided.");
                    }

                    var leadcontact = await CreateContactIfDetailsExistAsync(
                        client,
                        accessToken,
                        new Guid(accountId),
                        model.LeadId,
                        model.CompanyName,
                        model.CompanyOwnerLastName,
                        model.Email,
                        model.MainPhone,
                        model.CompanyOwnerRole
                    );
                    if (!string.IsNullOrEmpty(leadcontact))
                    {
                        var updateAccountJson = new JObject
                        {
                            ["primarycontactid@odata.bind"] = $"/contacts({leadcontact})"
                        };
                        await client.PatchAsync($"{CRMUrl}({accountId})", new StringContent(updateAccountJson.ToString(), Encoding.UTF8, "application/json"));

                    }
                    var contactId = await CreateContactIfDetailsExistAsync(
                        client,
                        accessToken,
                        new Guid(accountId),
                        model.LeadId,
                        model.PersonInChargeFirstName,
                        model.PersonInChargeLastName,
                        model.PersonInChargeEmailID,
                        model.PersonInChargePhoneNumber,
                        model.PersonInChargeRole
                    );

                    if (!string.IsNullOrEmpty(contactId))
                    {
                        if (!string.IsNullOrEmpty(contactId))
                        {
                            var updateAccountJson = new JObject
                            {
                                ["primarycontactid@odata.bind"] = $"/contacts({contactId})"
                            };
                            await client.PatchAsync($"{CRMUrl}({accountId})", new StringContent(updateAccountJson.ToString(), Encoding.UTF8, "application/json"));

                        }
                        if (!string.IsNullOrEmpty(model.CompanyOwnerFirstName) || !string.IsNullOrEmpty(model.CompanyOwnerLastName) || !string.IsNullOrEmpty(model.CompanyOwnerPhoneNumber) || !string.IsNullOrEmpty(model.CompanyOwnerEmailID))
                        {
                            var ownerContactId = await CreateContactIfDetailsExistAsync(
                                client,
                                accessToken,
                                new Guid(accountId),
                                model.LeadId,
                               model.CompanyOwnerFirstName,
                               model.CompanyOwnerLastName,
                               model.CompanyOwnerEmailID,
                               model.CompanyOwnerPhoneNumber,
                               model.CompanyOwnerRole
                               );
                            Thread.Sleep( 10000 );
                            if (!string.IsNullOrEmpty(ownerContactId))
                            {
                                var updateOwnerJson = new JObject
                                {
                                    ["nw_Owner@odata.bind"] = $"/contacts({ownerContactId})"
                                    
                                };
                                var patchResponse = await client.PatchAsync($"{CRMUrl}({accountId})", new StringContent(updateOwnerJson.ToString(), Encoding.UTF8, "application/json"));
                                if (!patchResponse.IsSuccessStatusCode)
                                {
                                    var error = await patchResponse.Content.ReadAsStringAsync();
                                    Console.WriteLine($"❌ Failed to update nw_owner: {error}");
                                    Console.WriteLine($"❌ update Json:"+ updateOwnerJson.ToString());
                                    Console.WriteLine($"❌ update Json:" + ownerContactId.ToString());
                                    //throw new Exception("Failed to update nw_owner on account");
                                    Console.WriteLine($"❌ Failed to update nw_owner UpdateJSON: {updateOwnerJson.ToString()}");// Line needs to be removed 
                                    throw new Exception("Failed to update nw_owner on account :" + error);
                                }
                                else
                                {
                                    Console.WriteLine("✅ Successfully updated nw_owner on account.");
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(model.PurchasingPersonFirstName) || !string.IsNullOrEmpty(model.PurchasingPersonLastName) || !string.IsNullOrEmpty(model.PurchasingPersonPhoneNumber) || !string.IsNullOrEmpty(model.PurchasingPersonEmailID))
                        {
                                var PurchasingPerson = await CreateContactIfDetailsExistAsync(client,
                                accessToken,
                                new Guid(accountId),
                               model.LeadId,
                               model.PurchasingPersonFirstName,
                               model.PurchasingPersonLastName,
                               model.PurchasingPersonEmailID,
                               model.PurchasingPersonPhoneNumber,
                               model.PurchasingPersonRole)
                               ;
                                if (!string.IsNullOrEmpty(PurchasingPerson))
                                {
                                    var UpdatePurchasingPersonJson = new JObject
                                    {
                                        ["nw_Purchasingperson@odata.bind"] = $"/contacts({PurchasingPerson})"
                                    };
                                    var patchresponse1 = await client.PatchAsync($"{CRMUrl}({accountId})", new StringContent(UpdatePurchasingPersonJson.ToString(), Encoding.UTF8, "application/json"));
                                    if (!patchresponse1.IsSuccessStatusCode)
                                    {
                                        var error = await patchresponse1.Content.ReadAsStringAsync();
                                        Console.WriteLine($"❌ Failed to update nw_owner: {error}");
                                        throw new Exception("Failed to update nw_owner on account");
                                    
                                }
                                    else
                                    {
                                        Console.WriteLine("✅ Successfully updated nw_owner on account.");
                                    }
                                }
                         }
                    }

                }

                return true;

            }

        }

        public async Task<bool> UpdateAccountBasedOnLeadId(
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
            byte[] EstablishmentCardFileData, string EstablishmentCardFileName, bool canUpdateLeadStatus) 
        {
            var accessToken = await GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                return false;
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.Add("OData-Version", "4.0");
                client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");

                // Fetch the associated account for the given leadId
                var accountId = await GetaccountID(leadId);
                bool isNewAccount = string.IsNullOrEmpty(accountId);

                // Fetch the owner of the lead
                var leadOwnerId = await GetLeadOwnerId(client, leadId);

                // Get the lookup values (payment method, segments, etc.)
                var BranchID = await GetLookupField(client, "cdm_companies", "cdm_company", "cdm_companycode", model.Branch);
                var paymentMethodId = await GetPaymentMethodIdByName(client, model.CustomerPaymentMethod, BranchID);
                var statisticGroupId = await GetLookupFieldIdByName(client, "nw_statisticgroups", "nw_statisticgroup", "nw_name", model.StatisticGroup, BranchID);
                var chefSegmentId = await GetLookupFieldIdByName(client, "nw_chefsegments", "nw_chefsegment", "nw_name", model.ChefSegment, BranchID);
                var subSegmentId = await GetLookupFieldIdByName(client, "nw_subsegments", "nw_subsegment", "nw_name", model.SubSegment, BranchID);
                var ClassificationID = await GetLookupFieldIdByName(client, "nw_classifications", "nw_classification", "nw_name", model.Classification, BranchID);
                var CorporateCountryID = await GetLookupField(client, "nw_countries", "nw_country", "nw_description", model.CorporateCountry);
                var CorporateCityID = await GetLookupField(client, "nw_cities", "nw_city", "nw_name", model.CorporateCity);
                string DeliveryCountryID = null;
                string DeliveryCityID = null;
                string RegisteredCountryID = null;
                string RegisteredCityID = null;
                if (!string.IsNullOrWhiteSpace(model.DeliveryCountry))
                    DeliveryCountryID = await GetLookupField(client, "nw_countries", "nw_country", "nw_description", model.DeliveryCountry);

                if (!string.IsNullOrWhiteSpace(model.DeliveryCity))
                    DeliveryCityID = await GetLookupField(client, "nw_cities", "nw_city", "nw_name", model.DeliveryCity);

                if (!string.IsNullOrWhiteSpace(model.RegisteredCountry))
                    RegisteredCountryID = await GetLookupField(client, "nw_countries", "nw_country", "nw_description", model.RegisteredCountry);

                if (!string.IsNullOrWhiteSpace(model.RegisteredCity))
                    RegisteredCityID = await GetLookupField(client, "nw_cities", "nw_city", "nw_name", model.RegisteredCity);

                if (string.IsNullOrEmpty(paymentMethodId) || string.IsNullOrEmpty(statisticGroupId) ||
                    string.IsNullOrEmpty(chefSegmentId) || string.IsNullOrEmpty(subSegmentId))
                {
                    throw new Exception("One or more required lookup fields were not found.");
                }

                JObject jsonAccount = new JObject
                {
                    ["name"] = model.CompanyName,
                    ["telephone1"] = model.MainPhone,
                    ["emailaddress1"] = model.Email,
                    ["msdyn_customerpaymentmethod@odata.bind"] = $"/msdyn_customerpaymentmethods({paymentMethodId})",
                    ["msdyn_company@odata.bind"] = $"/cdm_companies({BranchID})",
                    ["nw_StatisticGroup@odata.bind"] = $"/nw_statisticgroups({statisticGroupId})",
                    ["nw_ChefSegment@odata.bind"] = $"/nw_chefsegments({chefSegmentId})",
                    ["nw_SubSegment@odata.bind"] = $"/nw_subsegments({subSegmentId})",
                    ["nw_Classification1@odata.bind"] = $"/nw_classifications({ClassificationID})",
                    ["paymenttermscode"] = 6,
                    ["nw_tradenameoutletname"] = model.TradeName,
                    ["nw_tradelicensenumber"] = model.TradeLicenseNumber,
                    ["nw_licenseexpirydate"] = model.LicenseExpiryDate,
                    ["nw_vatnumber"] = model.VatNumber,
                    ["nw_iscontactpersonsameaspurchasing"] = model.IsContactPersonSameAsPurchasing == "1" ? true : false,
                    ["nw_scontactpersonsameascompanyowner"] = model.IsContactPersonSameAsCompanyOwner == "1" ? true : false,
                    ["nw_issametocorporateaddress"] = model.IsSameAsCorporateAddress == "1" ? true : false,
                    ["address1_line1"] = model.CorporateStreet,
                    ["shippingmethodcode"] = int.Parse(model.CorporateShippingMethod),
                    ["address2_line1"] = model.DeliveryStreet,
                    ["address2_shippingmethodcode"] = int.Parse(model.DeliveryShippingMethod),
                    ["nw_address3street"] = model.RegisteredStreet,
                    ["nw_estimatedpurchasevalue"] = model.EstimatedPurchaseValue,
                    ["nw_estimatedmonthlypurchaseaed"] = model.EstimatedMonthlyPurchase,
                    ["nw_amountofsecuritychequeamountaed"] = model.SecurityChequeAmount,
                    ["nw_proposecreditlimit"] = model.CreditLimit,
                    ["nw_proposecreditlimit1"] = model.RequestedCreditLimit,
                    ["new_isecommerce"] = model.Ecomerce,
                    ["new_reason"] = model.Reason,
                    ["new_ifusinginventorysystem"] = model.InventorySystem,
                    ["nw_establishmentcardnumber"] = model.EstablishmentCardNumber,
                    ["originatingleadid@odata.bind"] = $"/leads/{leadId}",
                    ["ownerid@odata.bind"] = $"/systemusers({leadOwnerId})"
                };

                if (!string.IsNullOrWhiteSpace(model.ProposedPaymentTerms))
                {
                    jsonAccount["nw_proposeanotherterm"] = true;
                    jsonAccount["nw_proposedpaymentterms"] = model.ProposedPaymentTerms;
                }

                if (!string.IsNullOrEmpty(CorporateCountryID))
                    jsonAccount["nw_Country11@odata.bind"] = $"/nw_countries({CorporateCountryID})";

                if (!string.IsNullOrEmpty(CorporateCityID))
                    jsonAccount["nw_City@odata.bind"] = $"/nw_cities({CorporateCityID})";

                if (!string.IsNullOrEmpty(RegisteredCountryID))
                    jsonAccount["nw_Countries@odata.bind"] = $"/nw_countries({RegisteredCountryID})";

                if (!string.IsNullOrEmpty(DeliveryCountryID))
                    jsonAccount["nw_DeliveryCountry@odata.bind"] = $"/nw_countries({DeliveryCountryID})";

                if (!string.IsNullOrEmpty(DeliveryCityID))
                    jsonAccount["nw_DeliveryCity@odata.bind"] = $"/nw_cities({DeliveryCityID})";

                if (!string.IsNullOrEmpty(RegisteredCityID))
                    jsonAccount["nw_Cityregisteredaddress@odata.bind"] = $"/nw_cities({RegisteredCityID})";

                var payload = JsonConvert.SerializeObject(jsonAccount);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                HttpResponseMessage response;

                if (isNewAccount)
                {
                    response = await client.PostAsync(CRMUrl, content);
                    if (response.IsSuccessStatusCode && response.Headers.Location != null)
                    {
                        var createdUri = response.Headers.Location;
                        var lastSegment = createdUri.Segments.Last().Trim('/');

                        // Handle cases where it's in the form accounts(GUID)
                        var match = Regex.Match(lastSegment, @"\(([^)]+)\)");
                        accountId = match.Success ? match.Groups[1].Value : lastSegment;

                        Trace.WriteLine("✅ Account created with ID: " + accountId);
                    }

                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"❌ Failed to create account: {error}");
                    }
                }
                else
                {
                    response = await client.PatchAsync($"{CRMUrl}({accountId})", content);
                }

                //var response = await client.PatchAsync($"{CRMUrl}({accountId})", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(accountId))
                {
                    Guid recordGuid = new Guid(accountId); // Convert accountId to GUID
                    string entityName = "account"; // Ensure this matches your entity name

                    // Dictionary containing file fields and their corresponding data
                    Dictionary<string, (byte[] fileData, string fileName)> fileFields = new Dictionary<string, (byte[], string)>
                    {
                        { "nw_vattrnattachcertificate", (vatFileData, vatFileName) },  // VAT Certificate
                        { "nw_tradecommerciallicensenoattachlicense", (tradeLicenseData, tradeLicenseName) },  // Trade License
                        { "nw_accountopeningfile", (accountOpeningFileData, accountOpeningFileName) },  // Account Opening File
                        { "nw_powerofattorney",(poaData,poaFileName) },
                        { "nw_passport" ,(passportData,passportFileName)},
                        { "nw_visa" ,(visaData,visaFileName)},
                        { "nw_emiratesidcard" ,(eidData,eidFileName)},
                        { "nw_checkcopy" ,(chequeFileData,chequeFileName)},
                        {"nw_establishmentcardcopy",(EstablishmentCardFileData,EstablishmentCardFileName)},

                    };

                    foreach (var fileEntry in fileFields)
                    {
                        string fileAttribute = fileEntry.Key;
                        byte[] fileData = fileEntry.Value.fileData;
                        string fileName = fileEntry.Value.fileName;

                        if (fileData != null && !string.IsNullOrEmpty(fileName))
                        {
                            try
                            {
                                await UploadFileToCRM(recordGuid, entityName, fileAttribute, fileData, fileName);
                                System.Diagnostics.Trace.WriteLine($"✅ {fileName} successfully uploaded to '{fileAttribute}'.");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine($"⚠ Failed to attach {fileName}: {ex.Message}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Trace.WriteLine($"⚠ Skipping upload for '{fileAttribute}' as file data is missing.");
                        }
                    }
                }
                var leadcontact = await CreateContactIfDetailsExistAsync(
                        client,
                        accessToken,
                        new Guid(accountId),
                        model.LeadId,
                        model.CompanyName,
                        null,
                        model.Email,
                        model.MainPhone,
                        null
                    );
                if (!string.IsNullOrEmpty(leadcontact))
                {
                    var updateAccountJson = new JObject
                    {
                        ["primarycontactid@odata.bind"] = $"/contacts({leadcontact})"
                    };
                    await client.PatchAsync($"{CRMUrl}({accountId})", new StringContent(updateAccountJson.ToString(), Encoding.UTF8, "application/json"));

                }
                if (!string.IsNullOrEmpty(accountId))
                {
                    if (!string.IsNullOrWhiteSpace(model.IBNAccountNumber) ||
                        !string.IsNullOrWhiteSpace(model.Bank) ||
                        !string.IsNullOrWhiteSpace(model.BankName) ||
                        !string.IsNullOrWhiteSpace(model.BankAddress) ||
                        !string.IsNullOrWhiteSpace(model.SwiftCode) ||
                        !string.IsNullOrWhiteSpace(model.IbanNumber))
                    {
                        var bankAccountJson = new JObject
                        {
                            ["nw_name"] = model.IBNAccountNumber,
                            ["nw_bank"] = model.Bank,
                            ["nw_bankname"] = model.BankName,
                            ["nw_bankaddress"] = model.BankAddress,
                            ["nw_swiftcode"] = model.SwiftCode,
                            ["nw_ibannumber"] = model.IbanNumber,
                            ["nw_Accountid@odata.bind"] = $"/accounts({accountId})"
                        };

                        System.Diagnostics.Trace.WriteLine(bankAccountJson);

                        var bankAccountResponse = await client.PostAsync(
                            CRMAppUrl+"/api/data/v9.2/nw_bankaccounts",
                            new StringContent(bankAccountJson.ToString(), Encoding.UTF8, "application/json")
                        );

                        System.Diagnostics.Trace.WriteLine(bankAccountResponse);

                        if (bankAccountResponse.IsSuccessStatusCode && bankAccountResponse.Headers.Location != null)
                        {
                            var bankAccountUri = bankAccountResponse.Headers.Location;
                            var lastSegment = bankAccountUri.Segments.Last().Trim('/');
                            var match = Regex.Match(lastSegment, @"\(([^)]+)\)");
                            var bankAccountId = match.Success ? match.Groups[1].Value : lastSegment;

                            // Step 2: Update Account with Bank Account lookup
                            var updateAccountWithBank = new JObject
                            {
                                ["nw_BankAccount@odata.bind"] = $"/nw_bankaccounts({bankAccountId})"
                            };

                            await client.PatchAsync($"{CRMUrl}({accountId})", new StringContent(updateAccountWithBank.ToString(), Encoding.UTF8, "application/json"));
                        }
                        else
                        {
                            var error = await bankAccountResponse.Content.ReadAsStringAsync();
                            Trace.WriteLine("⚠️ Failed to create bank account: " + error);
                        }
                    }
                    else
                    {
                        Trace.WriteLine("ℹ️ Skipping bank account creation – no bank details provided.");
                    }

                    var contactId = await CreateContactIfDetailsExistAsync(client,
                        accessToken,
                        new Guid(accountId),
                        model.LeadId,
                        model.PersonInChargeFirstName,
                        model.PersonInChargeLastName,
                        model.PersonInChargeEmailID,
                        model.PersonInChargePhoneNumber,
                        model.PersonInChargeRole);

                    if (!string.IsNullOrEmpty(contactId))
                    {
                        if (!string.IsNullOrEmpty(contactId))
                        {
                            var updateAccountJson = new JObject
                            {
                                ["primarycontactid@odata.bind"] = $"/contacts({contactId})"
                            };
                            await client.PatchAsync($"{CRMUrl}({accountId})", new StringContent(updateAccountJson.ToString(), Encoding.UTF8, "application/json"));

                        }
                        if (!string.IsNullOrEmpty(model.CompanyOwnerFirstName) || !string.IsNullOrEmpty(model.CompanyOwnerLastName) || !string.IsNullOrEmpty(model.CompanyOwnerPhoneNumber) || !string.IsNullOrEmpty(model.CompanyOwnerEmailID))
                        {
                            var ownerContactId = await CreateContactIfDetailsExistAsync(client,
                                accessToken,
                                new Guid(accountId),
                                model.LeadId,
                               model.CompanyOwnerFirstName,
                               model.CompanyOwnerLastName,
                               model.CompanyOwnerEmailID,
                               model.CompanyOwnerPhoneNumber,
                               model.CompanyOwnerRole);
                            Thread.Sleep(10000);
                            if (!string.IsNullOrEmpty(ownerContactId))
                            {
                                var updateOwnerJson = new JObject
                                {
                                    ["nw_Owner@odata.bind"] = $"/contacts({ownerContactId})"
                                };
                                var patchResponse = await client.PatchAsync($"{CRMUrl}({accountId})", new StringContent(updateOwnerJson.ToString(), Encoding.UTF8, "application/json"));
                                if (!patchResponse.IsSuccessStatusCode)
                                {
                                    var error = await patchResponse.Content.ReadAsStringAsync();
                                    Console.WriteLine($"❌ Failed to update nw_owner: {error}");
                                    Console.WriteLine($"❌ Failed to update nw_owner UpdateJSON: {updateOwnerJson.ToString()}");// Line needs to be removed 
                                    throw new Exception("Failed to update nw_owner on account :" + error);
                                }
                                else
                                {
                                    Console.WriteLine("✅ Successfully updated nw_owner on account.");
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(model.PurchasingPersonFirstName) || !string.IsNullOrEmpty(model.PurchasingPersonLastName) || !string.IsNullOrEmpty(model.PurchasingPersonPhoneNumber) || !string.IsNullOrEmpty(model.PurchasingPersonEmailID))    
                        {
                            var PurchasingPerson = await CreateContactIfDetailsExistAsync(client,   
                            accessToken,
                            new Guid(accountId),
                            model.LeadId,
                           model.PurchasingPersonFirstName,
                           model.PurchasingPersonLastName,
                           model.PurchasingPersonEmailID,
                           model.PurchasingPersonPhoneNumber,
                           model.PurchasingPersonRole);
                            if (!string.IsNullOrEmpty(PurchasingPerson))
                            {
                                var UpdatePurchasingPersonJson = new JObject
                                {
                                    ["nw_Purchasingperson@odata.bind"] = $"/contacts({PurchasingPerson})"
                                };
                                var patchresponse1 = await client.PatchAsync($"{CRMUrl}({accountId})", new StringContent(UpdatePurchasingPersonJson.ToString(), Encoding.UTF8, "application/json"));
                                if (!patchresponse1.IsSuccessStatusCode)
                                {
                                    var error = await patchresponse1.Content.ReadAsStringAsync();
                                    Console.WriteLine($"❌ Failed to update nw_owner: {error}");
                                    throw new Exception("Failed to update nw_owner on account");
                                }
                                else
                                {
                                    Console.WriteLine("✅ Successfully updated nw_owner on account.");
                                }
                            }
                        }
                    }

                }
                //Update the Lead status once it is Submitted
                if (canUpdateLeadStatus)
                {
                    UpdateLeadStatusOnSubmission(leadId);
                }
                await CreateAndSendEmailAfterAccountUpdate(client, leadId,accountId,ClientId);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"CRM Update Account Failed: {responseContent} {payload}");
                }

                return true;
            }
        }

        public string GetUploadedFileNameByService(string accountId, string fileattachmentid)
        {
            try
            {
                string crmUrl = ConfigurationManager.AppSettings["CRMAppUrl"];
                string clientId = ConfigurationManager.AppSettings["CRMClientId"];
                string tenantId = ConfigurationManager.AppSettings["CRMTenantId"];
                string clientSecret = ConfigurationManager.AppSettings["CRMClientSecret"];

                string connectionString = $"AuthType=ClientSecret;Url={crmUrl};ClientId={clientId};ClientSecret={clientSecret};TenantId={tenantId}";

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var serviceClient = new ServiceClient(connectionString);

                string fetchXml = string.Format(@"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='fileattachment'>
                        <attribute name='filename' />
                        <filter type='and'>
                          <condition attribute='fileattachmentid' operator='eq' value='{0}' />
                        </filter>
                      </entity>
                    </fetch>",fileattachmentid);

                EntityCollection results = serviceClient.RetrieveMultiple(new FetchExpression(fetchXml));
                string fileName = string.Empty;
                foreach (var entity in results.Entities)
                {
                    fileName = entity.GetAttributeValue<string>("filename");
                    return fileName;
                }

                serviceClient.Dispose();

                return fileName;
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }

        private void UpdateLeadStatusOnSubmission(string leadId)
        {
            try
            {
                string crmUrl = ConfigurationManager.AppSettings["CRMAppUrl"];
                string clientId = ConfigurationManager.AppSettings["CRMClientId"];
                string tenantId = ConfigurationManager.AppSettings["CRMTenantId"];
                string clientSecret = ConfigurationManager.AppSettings["CRMClientSecret"];

                string connectionString = $"AuthType=ClientSecret;Url={crmUrl};ClientId={clientId};ClientSecret={clientSecret};TenantId={tenantId}";

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var serviceClient = new ServiceClient(connectionString);

                var leadRecord = new Entity("lead");
                leadRecord.Id = new Guid(leadId);
                leadRecord["statuscode"] = new OptionSetValue(920750002);
                serviceClient.Update(leadRecord);
                serviceClient.Dispose();
            }
            catch (Exception e)
            {

            }
        }
        
        public async Task CreateAndSendEmailAfterAccountUpdate(HttpClient client, string leadId, string accountId, string appClientId)
        {
            var parsedLeadId = leadId.Trim('{', '}');
            var parsedAccountId = accountId.Trim('{', '}');

            // 1. Get lead details
            var leadUrl = $"{CRMAppUrl}/api/data/v9.2/leads({parsedLeadId})?$select=emailaddress1,fullname,_ownerid_value";
            var leadResponse = await client.GetAsync(leadUrl);
            if (!leadResponse.IsSuccessStatusCode)
                throw new Exception("Failed to get lead details: " + await leadResponse.Content.ReadAsStringAsync());

            var leadJson = JObject.Parse(await leadResponse.Content.ReadAsStringAsync());
            var leadFullName = leadJson["fullname"]?.ToString();
            var ownerId = leadJson["_ownerid_value"]?.ToString();

            if (string.IsNullOrEmpty(ownerId))
                throw new Exception("Missing owner ID from lead.");

            // 2. Get lead owner's systemuser details
            var ownerUrl = $"{CRMAppUrl}/api/data/v9.2/systemusers({ownerId})?$select=internalemailaddress,fullname";
            var ownerResponse = await client.GetAsync(ownerUrl);
            if (!ownerResponse.IsSuccessStatusCode)
                throw new Exception("Failed to get owner details: " + await ownerResponse.Content.ReadAsStringAsync());

            var ownerJson = JObject.Parse(await ownerResponse.Content.ReadAsStringAsync());
            var ownerFullName = ownerJson["fullname"]?.ToString();
            var accountUrl = $"{CRMAppUrl}/main.aspx?appid=a5263f90-06af-ec11-9841-0022489dd690&pagetype=entityrecord&etn=account&id={parsedAccountId}";
            // 4. Create the email body
            var emailBody = $@"
            Dear {ownerFullName},<br/><br/>
            A customer linked to lead <b>{leadFullName}</b> has completed their account update.<br/><br/>
            You can now proceed to review their account by clicking the following link: <a href='{accountUrl}'>{accountUrl}</a><br/><br/>
            Best regards,<br/>
            CRM System
            ";

            // 5. Construct email payload
            var emailPayload = new JObject
            {
                ["subject"] = $"Lead {leadFullName} account updated",
                ["description"] = emailBody,
                ["directioncode"] = true,
                ["scheduledstart"] = DateTime.UtcNow,
                ["scheduledend"] = DateTime.UtcNow.AddHours(1),
                ["regardingobjectid_account@odata.bind"] = $"/accounts({parsedAccountId})",
                ["email_activity_parties"] = new JArray
        {
            new JObject
            {
                ["partyid_systemuser@odata.bind"] = $"/systemusers(4aef952b-2fa9-ed11-aad1-000d3ab47884)", 
                ["participationtypemask"] = 1
            },
            new JObject
            {
                ["partyid_systemuser@odata.bind"] = $"/systemusers({ownerId})", // TO: Owner
                ["participationtypemask"] = 2
            }
        }
            };

            var request = new HttpRequestMessage(HttpMethod.Post,
                CRMAppUrl+"/api/data/v9.2/emails")
            {
                Content = new StringContent(emailPayload.ToString(), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Prefer", "return=representation");

            var createEmailResponse = await client.SendAsync(request);
            var emailResponseContent = await createEmailResponse.Content.ReadAsStringAsync();
            Trace.WriteLine(emailResponseContent);

            if (!createEmailResponse.IsSuccessStatusCode)
                throw new Exception("Error creating email activity: " + emailResponseContent);

            var emailJson = JObject.Parse(emailResponseContent);
            var emailId = emailJson["activityid"]?.ToString();

            if (string.IsNullOrEmpty(emailId))
                throw new Exception("Failed to retrieve email ID.");

            // 6. Send the email
            var sendPayload = new JObject { ["IssueSend"] = true };
            var sendResponse = await client.PostAsync(
                $"{CRMAppUrl}/api/data/v9.2/emails({emailId})/Microsoft.Dynamics.CRM.SendEmail",
                new StringContent(sendPayload.ToString(), Encoding.UTF8, "application/json")
            );

            if (!sendResponse.IsSuccessStatusCode)
                throw new Exception("Error sending email: " + await sendResponse.Content.ReadAsStringAsync());

            Console.WriteLine("✅ Email sent successfully from application user to lead owner.");
        }

        public async Task<string> CreateContactIfDetailsExistAsync(
            HttpClient client,
            string accessToken,
            Guid accountId,
            string leadId, // Moved leadId before firstName
            string firstName,
            string lastName,
            string email,
            string phone,
            string role)
        {
            // Check if first name or last name is provided
            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
                return null;

            // Validate Lead ID
            if (!Guid.TryParse(leadId, out Guid leadGuid))
            {
                throw new Exception("Invalid Lead ID format.");
            }

            // Fetch Lead Owner ID
            string leadOwnerId = await GetLeadOwnerId(client, leadId);

            // Check if the contact already exists (by email or phone)
            string existingContactId = await GetExistingContactIdByEmailOrPhoneAsync(accessToken, email, phone);
            if (!string.IsNullOrEmpty(existingContactId))
            {
                return existingContactId; // Return the existing contact ID
            }

            // Create new contact if no duplicate is found
            var contact = new Dictionary<string, object>
            {
                { "firstname", string.IsNullOrWhiteSpace(firstName) ? "" : firstName },
                { "lastname", string.IsNullOrWhiteSpace(lastName) ? "" : lastName },
                { "parentcustomerid_account@odata.bind", $"/accounts({accountId})" },
                { "ownerid@odata.bind", $"/systemusers({leadOwnerId})" } // Assigning contact owner as lead owner
            };

            if (!string.IsNullOrWhiteSpace(email))
                contact["emailaddress1"] = email;

            if (!string.IsNullOrWhiteSpace(phone))
                contact["mobilephone"] = phone;

            if (!string.IsNullOrWhiteSpace(role))
                contact["nw_rule"] = role; // Assuming nw_rule is the role field in CRM

            var jsonContent = new StringContent(JsonConvert.SerializeObject(contact), Encoding.UTF8, "application/json");

            // Use the passed HttpClient instead of creating a new one
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.PostAsync(CRMAppUrl+"/api/data/v9.2/contacts", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var contactUri = response.Headers.Location?.ToString();
                if (contactUri != null)
                {
                    var contactId = contactUri.Split('(', ')')[1];
                    return contactId;
                }
            }

            var error = await response.Content.ReadAsStringAsync();
            throw new Exception("Contact creation failed: " + error);
        }


        public async Task<string> GetExistingContactIdByEmailOrPhoneAsync(string accessToken, string email, string phone)
        {
            // Build the query to check for existing contact by email or phone
            var query = $"contacts?$filter=emailaddress1 eq '{email}' or mobilephone eq '{phone}'";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                httpClient.BaseAddress = new Uri(CRMAppUrl +"/api/data/v9.2/");

                var response = await httpClient.GetAsync(query);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var contacts = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    if (contacts.value.Count > 0)
                    {
                        // Contact already exists, return the contact ID
                        return contacts.value[0].contactid.ToString();
                    }
                }

                return null; // No contact found
            }
        }



        public async Task<string> GetaccountID(string leadId)
        {
            var accessToken = await GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new Exception("Failed to retrieve access token.");
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.Add("OData-Version", "4.0");
                client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");

                // Build the OData query to retrieve the accountId for the Account linked to the leadId via originatingleadid
                string odataUrl = $"{ConfigurationManager.AppSettings["CRMAppUrl"]}/api/data/v9.2/accounts?" +
                    "$filter=_originatingleadid_value eq " + leadId + "&" +
                    "$select=" +
                        "accountid,";

                try
                {
                    var response = await client.GetAsync(odataUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var jsonResponse = JObject.Parse(responseContent);
                        var accountId = jsonResponse["value"]?.First?["accountid"]?.ToString(); // Get account ID from the response
                        return accountId;

                        if (!string.IsNullOrEmpty(accountId))
                        {
                            return accountId; // Return the accountId
                        }
                        else
                        {
                            throw new Exception("No account found for the provided leadId.");
                        }
                    }
                    else
                    {
                        throw new Exception($"Failed to retrieve account for leadId {leadId}. Status: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error retrieving account ID for leadId {leadId}: {ex.Message}");
                }
            }
        }

        public async Task<string> GetLeadOwnerId(HttpClient client, string leadId)
        {
            var accessToken = await GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
                throw new Exception("Failed to retrieve access token.");

            if (!Guid.TryParse(leadId, out var parsedLeadId))
                throw new ArgumentException("Invalid Lead ID format.");

            string requestUrl = $"{CRMAppUrl}/api/data/v9.2/leads({parsedLeadId})?$select=_ownerid_value";

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Trace.WriteLine("Requesting URL: " + requestUrl);

            try
            {
                var response = await client.GetAsync(requestUrl);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Trace.WriteLine(response.StatusCode +  content);
                    throw new Exception($"Failed to retrieve lead owner. Status: {response.StatusCode}. Error: {content}");
                }

                var json = JObject.Parse(content);
                return json["_ownerid_value"]?.ToString() ?? throw new Exception("Owner ID not found.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving lead owner: {ex.Message}");
            }
        }


        public async Task UploadFileToCRM(Guid recordId, string entityName, string fileAttribute, byte[] fileData, string fileName)
        {
            if (fileData == null || fileData.Length == 0)
                return;

            try
            {
                string crmUrl = ConfigurationManager.AppSettings["CRMAppUrl"];
                string clientId = ConfigurationManager.AppSettings["CRMClientId"];
                string tenantId = ConfigurationManager.AppSettings["CRMTenantId"];
                string clientSecret = ConfigurationManager.AppSettings["CRMClientSecret"];

                string connectionString = $"AuthType=ClientSecret;Url={crmUrl};ClientId={clientId};ClientSecret={clientSecret};TenantId={tenantId}";

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var serviceClient = new ServiceClient(connectionString);

                //CrmServiceClient svc = new CrmServiceClient(connectionString);

                var initializeFileBlocksUploadRequest = new InitializeFileBlocksUploadRequest()
                {
                    Target = new EntityReference(entityName, recordId),
                    FileAttributeName = fileAttribute,
                    FileName = fileName
                };

                var initializeFileBlocksUploadResponse = (InitializeFileBlocksUploadResponse)serviceClient.Execute(initializeFileBlocksUploadRequest);
                var fileContinuationToken = initializeFileBlocksUploadResponse.FileContinuationToken;
                var blockList = new List<string>();

                int chunkSize = 4194304; // 4MB
                for (int i = 0; i < fileData.Length; i += chunkSize)
                {
                    var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
                    blockList.Add(blockId);

                    var uploadBlockRequest = new UploadBlockRequest()
                    {
                        BlockId = blockId,
                        BlockData = fileData.Skip(i).Take(chunkSize).ToArray(),
                        FileContinuationToken = fileContinuationToken
                    };

                    serviceClient.Execute(uploadBlockRequest);
                }

                var commitFileBlocksUploadRequest = new CommitFileBlocksUploadRequest
                {
                    FileContinuationToken = fileContinuationToken,
                    FileName = fileName,
                    MimeType = MimeMapping.GetMimeMapping(fileName),
                    BlockList = blockList.ToArray()
                };

                serviceClient.Execute(commitFileBlocksUploadRequest);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("File Upload Error: " + ex.Message);
            }
        }
        public Task<bool> CreateAccountAsync(Form form)
        {
            throw new NotImplementedException();
        }
    }
}
