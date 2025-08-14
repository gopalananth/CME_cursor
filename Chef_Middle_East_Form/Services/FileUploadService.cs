using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Chef_Middle_East_Form.Services
{
    public static class FileUploadService
    {
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