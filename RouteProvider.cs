namespace Nop.Plugin.Misc.ImportProducts
{
    using System.Web.Mvc;
    using System.Web.Routing;
    using Nop.Web.Framework.Mvc.Routes;

    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("Plugin.Misc.ImportProducts",
                "Plugins/MiscImportProducts/Configure",
                new { controller = "MiscImportProducts", action = "Configure" },
                new[] { "Nop.Plugin.Misc.ImportProducts.Controllers" }
           );

        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
