using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Misc.ImportProducts.Controllers;
using Nop.Core.Infrastructure;
using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.ImportProducts.Model
{
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
        void ImportCentralaZabawek(Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.products productsToImport);
        Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.products LoadProductFromFileCentralaZabawek(string filePath);
        Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.products LoadProductsCentralaZabawek(importData importData);

        void ImportMotyle(Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.PRODUCTS productsToImport);
        Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.PRODUCTS LoadFromXMLFileMotyle(string filePath);
        //Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.products LoadProductsFromXSLFileMotyle(string filePath);
        Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.PRODUCTS LoadProductsFromXML(importData importaData);
        
    }
}
