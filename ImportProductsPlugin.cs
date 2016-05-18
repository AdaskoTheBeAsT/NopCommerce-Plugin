namespace Nop.Plugin.Misc.ImportProducts
{
    using System.Configuration;
    using System.Linq;
    using System.Web;
    using System.Web.Configuration;
    using System.Web.Routing;
    using Nop.Core.Plugins;
    using Nop.Services.Common;
    using Nop.Services.Localization;
    using Nop.Web.Framework.Menu;

    using SiteMapNode = Nop.Web.Framework.Menu.SiteMapNode;

    public class ImportProductsPlugin : BasePlugin,  IAdminMenuPlugin, IMiscPlugin
    {
        private readonly ILocalizationService _localizationService;
        private readonly ILanguageService _languageService;

        public ImportProductsPlugin(ILocalizationService localizationService, ILanguageService languageService)
        {
            _localizationService = localizationService;
            _languageService = languageService;
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "MiscImportProducts";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Misc.ImportProducts.Controllers" }, { "area", null } };
        }

        public override void Install()
        {
            InstallLocale();

            // InsertConfiguration();
            base.Install();
        }

        public override void Uninstall()
        {
            DeleteLocale();
            base.Uninstall();
        }

        public void ManageSiteMap(SiteMapNode rootNode)
        {
            var menuItem = new Web.Framework.Menu.SiteMapNode()
            {
                SystemName = "MiscImportProducts",
                Title = "Import",
                ControllerName = "MiscImportProducts",
                ActionName = "Import",
                Visible = true,
                RouteValues = new RouteValueDictionary() { { "area", null } },
            };

            var pluginNode = rootNode.ChildNodes.FirstOrDefault(x => x.SystemName == "Third party plugins");
            if (pluginNode != null)
            {
                pluginNode.ChildNodes.Add(menuItem);
            }
            else
            {
                rootNode.ChildNodes.Add(menuItem);
            }
        }

        private void InsertConfiguration()
        {
            var config = WebConfigurationManager.OpenWebConfiguration(HttpContext.Current.Request.ApplicationPath);
            var section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
            var sect = ConfigurationManager.GetSection("system.web");
            section.MaxRequestLength = 1048576; // read only - not works

            config.Save();
        }

        private void InstallLocale()
        {
            this.AddOrUpdatePluginLocaleResource("Plugin.Misc.ImportProducts.Required", "This field is required");
            this.AddOrUpdatePluginLocaleResource("Plugin.Misc.ImportProducts.Tax.Rate", "Rate");
        }

        private void DeleteLocale()
        {
            this.DeletePluginLocaleResource("Plugin.Misc.ImportProducts.Required");
            this.DeletePluginLocaleResource("Plugin.Misc.ImportProducts.Tax.Rate");
        }
    }
}
