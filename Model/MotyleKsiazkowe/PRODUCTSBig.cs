namespace Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe
{
    using System;
    using System.Collections.Generic;
    using System.Web.Mvc;
    using System.Xml.Serialization;
    using FluentValidation.Attributes;
    using Nop.Web.Framework;

    [Serializable]
    public class PRODUCT
    {
        public PRODUCT() { }

        [XmlElement("EAN")]
        public string Ean { get; set; }
        [XmlElement("PRICE")]
        public string Price { get; set; }
        [XmlElement("VAT")]
        public string Vat { get; set; }
        [AllowHtml]
        [XmlElement("DESCRIPTION")]
        public string Description { get; set; }
        [XmlElement("ORIGINAL_NAME")]
        public string Original_name { get; set; }
        [XmlElement("ISBN")]
        public string ISBN { get; set; }
        [XmlElement("PKWIU")]
        public string PKWIU { get; set; }
        [XmlElement("SERIES")]
        public string Series { get; set; }
        [XmlElement("NAME")]
        public string Name { get; set; }
        [XmlElement("COUNT_PAGES")]
        public string Count_pages { get; set; }
        [XmlElement("DIMENSIONS")]
        public string Dimensions { get; set; }
        [XmlElement("CATEGORIES")]
        public string Categories { get; set; }
        [XmlArray("PERSONS")]
        [XmlArrayItem("PERSON")]
        public string[] Persons { get; set; }
        [XmlArray("FIRMS")]
        [XmlArrayItem("FIRM")]
        public string[] Firms { get; set; }
        [XmlElement("STOCK")]
        public string Stock { get; set; }
        [XmlElement("URL_IMG")]
        public string Url_img { get; set; }

        public bool isNeeded { get; set; }
        public string[] categoryMapped { get; set; }
    }

    [Validator(typeof(ImportViewModelValidatorProductMotyle))]
    [Serializable, XmlRoot(ElementName = "PRODUCTS", DataType = "string", IsNullable = true)]
    public class PRODUCTS
    {
        public PRODUCTS() { }

        [XmlElement("PRODUCT")]
        public PRODUCT[] product { get; set; }

        [NopResourceDisplayName("Plugin.Misc.ImportProducts.Required")]
        public int priceIncrease { get; set; }

        public bool isLimitedToStores { get; set; }
        public List<storeChecked> shopsAddTo { get; set; }
        public List<string> mainCategoriesMapped { get; set; }
        public List<string> subCategoriesMapped { get; set; }
        public int categoryState { get; set; }
        public List<warehouseChecked> warehousesList { get; set; }
    }
}
