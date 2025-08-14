using System.Linq;
using System.Web.Mvc;
using Unity.AspNet.Mvc;
using Unity.Mvc5;  

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(Chef_Middle_East_Form.App_Start.UnityMvcActivator), nameof(Chef_Middle_East_Form.App_Start.UnityMvcActivator.Start))]
[assembly: WebActivatorEx.ApplicationShutdownMethod(typeof(Chef_Middle_East_Form.App_Start.UnityMvcActivator), nameof(Chef_Middle_East_Form.App_Start.UnityMvcActivator.Shutdown))]

namespace Chef_Middle_East_Form.App_Start
{
    public static class UnityMvcActivator
    {
        public static void Start()
        {
            var container = UnityConfig.Container;
            FilterProviders.Providers.Remove(FilterProviders.Providers.OfType<FilterAttributeFilterProvider>().First());
            FilterProviders.Providers.Add(new UnityFilterAttributeFilterProvider(container));
        }

        public static void Shutdown()
        {
            var container = UnityConfig.Container;
            container.Dispose();
        }
    }
}
