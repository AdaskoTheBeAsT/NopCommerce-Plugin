using FluentValidation.Attributes;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Plugin.Misc.ImportProducts.Model;
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

namespace Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe
{
    [Serializable]
    public class product
    {
        public product() { }

        [XmlElement("ean")]
        public string Ean { get; set; }
        [XmlElement("price")]
        public string Price { get; set; }
        [XmlElement("vat")]
        public string Vat { get; set; }
        [XmlElement("description")]
        public string Description { get; set; }
        [XmlElement("isbn")]
        public string ISBN { get; set; }
        [XmlElement("name")]
        public string Name { get; set; }
        [XmlElement("count_pages")]
        public string Count_pages { get; set; }
        [XmlElement("dimensions")]
        public string Dimensions { get; set; }
        [XmlElement("categories")]
        public string Categories { get; set; }

        //persons 
        //firms
        [XmlElement("stock")]
        public string Stock { get; set; }
        [XmlElement("url_img")]
        public string Url_img { get; set; }
        
        public bool isNeeded { get; set; }
        public string[] categoryMapped { get; set; }

    }

    [Validator(typeof(ImportViewModelValidatorProductMotyle))]
    [Serializable, XmlRoot(ElementName = "products", DataType = "string", IsNullable = true)]
    public class productszzzz
    {
        public productszzzz() { }

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
