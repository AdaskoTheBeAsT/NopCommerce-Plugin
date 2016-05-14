using Nop.Web.Framework.Mvc.Routes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;

namespace Nop.Plugin.Misc.ImportProducts
{
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
