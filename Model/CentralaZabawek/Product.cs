
using FluentValidation.Attributes;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Xml.Serialization;

namespace Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek
{
    [Serializable]
    public class product
    {
        public product() { }

        [XmlElement("name")]
        public string Name { get; set; }
        [XmlElement("ean")]
        public string Ean { get; set; }
        [XmlElement("price")]
        public string Price { get; set; }
        [XmlArray("images")]
        [XmlArrayItem("image")]
        public string[] picturePaths { get; set; }
        [AllowHtml]
        [XmlElement("description")]
        public string Description { get; set; }
        [XmlElement("vat")]
        public string Vat { get; set; }
        [XmlElement("weight")]
        public string Weight { get; set; }
        [XmlElement("stock")]
        public string Stock { get; set; }
        [XmlElement("category")]
        public string Category { get; set; }
        [XmlElement("manufacturer")]
        public string Manufacturer { get; set; }
        [XmlElement("guarantee")]
        public string Guarantee { get; set; }
        [XmlElement("sex")]
        public string Sex { get; set; }

        public bool isNeeded { get; set; }
        public string[] categoryMapped { get; set; }

    }

    [Validator(typeof(ImportViewModelValidatorProduct))]
    [Serializable, XmlRoot(ElementName = "products", DataType = "string", IsNullable = true)]
    public class products
    {
        public products() { }

        [XmlElement("product")]
        public product[] product { get; set; }

        [NopResourceDisplayName("Plugin.Misc.ImportProducts.Required")]
        public int priceIncrease { get; set; }

        public bool isLimitedToStores { get; set; }
        public List<storeChecked> shopsAddTo { get; set; }
        public List<string> mainCategoriesMapped { get; set; }
        public List<string> subCategoriesMapped { get; set; }
        public int categoryState { get; set; }
        public List<warehouseChecked> warehousesList { get; set; }
        //public PagedList<product> pagedProductList { get; set; }
    }

}
