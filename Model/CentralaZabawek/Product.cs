namespace Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek
{
    using System;
    using System.Collections.Generic;
    using System.Web.Mvc;
    using System.Xml.Serialization;
    using FluentValidation.Attributes;
    using Nop.Web.Framework;

    [Serializable]
    [XmlRoot("product")]
    public class Product
    {
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

        public bool IsNeeded { get; set; }

        public string[] CategoryMapped { get; set; }
    }

    [Validator(typeof(ImportViewModelValidatorProduct))]
    [Serializable]
    [XmlRoot(ElementName = "products", DataType = "string", IsNullable = true)]
    public class Products
    {
        [XmlElement("product")]
        public Product[] product { get; set; }

        [NopResourceDisplayName("Plugin.Misc.ImportProducts.Required")]
        public int priceIncrease { get; set; }

        public bool isLimitedToStores { get; set; }

        public List<storeChecked> shopsAddTo { get; set; }

        public List<string> mainCategoriesMapped { get; set; }

        public List<string> subCategoriesMapped { get; set; }

        public int categoryState { get; set; }

        public List<warehouseChecked> warehousesList { get; set; }

        // public PagedList<product> pagedProductList { get; set; }
    }
}
