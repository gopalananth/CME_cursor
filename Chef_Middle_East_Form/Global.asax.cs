using Chef_Middle_East_Form.App_Start;
using System.IO;
using System;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Chef_Middle_East_Form
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            UnityConfig.RegisterComponents();  // 💡 Register Unity here
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            //byte[] bytes = Chef_Middle_East_Form.Properties.Resources.Aspose_Words_NET;
            //using (MemoryStream stream = new MemoryStream(bytes))
            //{
            //    // Set license for Aspose.Cells
            //    Aspose.Words.License lic = new Aspose.Words.License();         
            //    lic.SetLicense(stream);
            //    // Now you have a stream
            //    // You can use the stream here
            //}
        }
    }
}
