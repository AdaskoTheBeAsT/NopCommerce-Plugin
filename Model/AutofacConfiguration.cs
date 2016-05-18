namespace Nop.Plugin.Misc.ImportProducts.Model
{
    using Autofac;
    using Nop.Core.Configuration;
    using Nop.Core.Infrastructure;
    using Nop.Core.Infrastructure.DependencyManagement;
    using Nop.Plugin.Misc.ImportProducts.Controllers;
    using Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek;
    using Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe;

    public class DependencyRegistrar : IDependencyRegistrar
    {
        public int Order
        {
            get
            {
                return 1;
            }
        }

        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            builder.RegisterType<ImportViewModel>().As<IImportService>().InstancePerLifetimeScope();
        }
    }

    public interface IImportService
    {
        void ImportCentralaZabawek(Products productsToImport);

        Products LoadProductFromFileCentralaZabawek(string filePath);

        Products LoadProductsCentralaZabawek(importData importData);

        void ImportMotyle(PRODUCTS productsToImport);

        PRODUCTS LoadFromXMLFileMotyle(string filePath);

        // Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.products LoadProductsFromXSLFileMotyle(string filePath);
        PRODUCTS LoadProductsFromXML(importData importaData);
    }
}
