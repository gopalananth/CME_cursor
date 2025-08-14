using Chef_Middle_East_Form.Services;
using System.Web.Mvc;
using Unity;
using Unity.Mvc5;

namespace Chef_Middle_East_Form.App_Start
{
    public static class UnityConfig
    {
        private static IUnityContainer container;

        public static IUnityContainer Container => container ?? (container = new UnityContainer());

        public static void RegisterComponents()
        {
            container = new UnityContainer();
            container.RegisterType<ICRMService, CRMService>();

            // Register your types here
            // e.g., container.RegisterType<IProductRepository, ProductRepository>();

            DependencyResolver.SetResolver(new UnityDependencyResolver(container));
        }
    }
}

