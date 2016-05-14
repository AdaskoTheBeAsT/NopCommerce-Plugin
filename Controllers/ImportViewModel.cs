using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Tax;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Misc.ImportProducts.Model;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Media;
using Nop.Services.Tax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using Nop.Web.Framework.Mvc;
using Nop.Core.Configuration;
using Nop.Web.Framework.Validators;
using Nop.Core.Domain.Localization;
using System.Xml;
using Nop.Services.Stores;
using Nop.Core.Domain.Stores;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Core.Domain.Shipping;
using Nop.Admin.Models.Catalog;
using Nop.Core;
using Nop.Services.Shipping;
using Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace Nop.Plugin.Misc.ImportProducts.Controllers
{
    public class ImportViewModel : IImportService
    {
        IPictureService _pictureService;
        IProductService _productService;
        ICategoryService _categoryService;
        IManufacturerService _manufacturerService;
        ITaxCategoryService _taxCategoryService;
        ITaxService _taxService;
        ISettingService _settingService;
        ILocalizationService _localizationService;
        IStoreMappingService _storeMapingService;
        ILogger _logger;
        IShippingService _shippingService;
        IShipmentService _shipmentService;
        ISpecificationAttributeService _specificationAttributeService;

        public ImportViewModel(IPictureService pictureService, IProductService productService, ICategoryService categoryService, IManufacturerService manufacturerService, ITaxCategoryService taxCategoryService, ITaxService taxService, ISettingService settingService, ILocalizationService localizationService, IStoreMappingService storeMapingService, ILogger logger, IShippingService shippingService, IShipmentService shipmentService, ISpecificationAttributeService specificationAttributeService)
        {
            this._pictureService = pictureService;
            this._productService = productService;
            this._categoryService = categoryService;
            this._manufacturerService = manufacturerService;
            this._taxCategoryService = taxCategoryService;
            this._taxService = taxService;
            this._settingService = settingService;
            this._localizationService = localizationService;
            this._storeMapingService = storeMapingService;
            this._logger = logger;
            this._shippingService = shippingService;
            this._shipmentService = shipmentService;
            this._specificationAttributeService = specificationAttributeService;
        }

        #region Centrala Zabawek
        Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.products pcz;
        XmlSerializer xmlSerializer;

        public Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.products LoadProductsCentralaZabawek(importData importData)
        {
            xmlSerializer = new XmlSerializer(typeof(Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.products));
            string newLink; string passwordChanged = importData.link;  //string important in change of link (replace)

            if (importData.link.Contains("login_partnera") && importData.link.Contains("haslo_partnera"))
            {
                //prepare link - replace oryginal link with login and password
                newLink = importData.link.Replace("login_partnera", importData.login.ToString()); passwordChanged = newLink.Replace("haslo_partnera", importData.password.ToString());
            }
            //ready link (URI) to get XML
            var targetUri = new Uri(passwordChanged);

            //Get XML from Webservice
            HttpWebRequest lxRequestt = (HttpWebRequest)WebRequest.Create(targetUri);

            using (HttpWebResponse lxResponsee = (HttpWebResponse)lxRequestt.GetResponse())
            {
                using (StreamReader reader = new StreamReader(lxResponsee.GetResponseStream()))
                {
                    //serialize XML from webservice to objects of class products
                    pcz = (Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.products)xmlSerializer.Deserialize(reader);
                }
            }

            return pcz;
        }

        public Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.products LoadProductFromFileCentralaZabawek(string filePath)
        {
            //XmlDocument doc = new XmlDocument();
            xmlSerializer = new XmlSerializer(typeof(Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.products));

            string path = AppDomain.CurrentDomain.BaseDirectory.ToString() + "Plugins\\Misc.ImportProducts\\Przykladowe.xml";
            //doc.Load(filePath);

            TextReader txtReader = new StreamReader(filePath);
            //// DeSerialize from the StreamReader
            pcz = (Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.products)xmlSerializer.Deserialize(txtReader);
            txtReader.Close();

            return pcz;
        }

        string[] categoriesXML;
        public void ImportCentralaZabawek(Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.products productsToImport)
        {
            try
            {
                Core.Domain.Catalog.Product prod = null;

                //add every product in list of products got from websevice XML
                foreach (Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.product p in productsToImport.product)
                {
                    if (p.isNeeded)
                    {
                        prod = new Core.Domain.Catalog.Product(); //create new product - it has to create new object
                        prod.Name = p.Name;
                        prod.ManufacturerPartNumber = p.Ean;
                        int i = 0;
                        //add images to product from list of images links
                        foreach (string picturePath in p.picturePaths)
                        {
                            var mimeType = GetMimeTypeFromFilePath(picturePath); //get type of image

                            //get image from URL on wholesale server
                            HttpWebRequest lxRequest = (HttpWebRequest)WebRequest.Create(picturePath);

                            String lsResponse = string.Empty;
                            using (HttpWebResponse lxResponse = (HttpWebResponse)lxRequest.GetResponse())
                            {
                                using (BinaryReader reader = new BinaryReader(lxResponse.GetResponseStream()))
                                {
                                    // make a binary table where image will be stored to put into database
                                    var newPictureBinary = reader.ReadBytes(1 * 1024 * 1024 * 10);
                                    //insert image into database
                                    var newPicture = _pictureService.InsertPicture(newPictureBinary, mimeType, _pictureService.GetPictureSeName(prod.Name));
                                    //map added picture to dabatese to the product
                                    prod.ProductPictures.Add(new ProductPicture
                                    {
                                        PictureId = newPicture.Id,
                                        DisplayOrder = i,
                                    });
                                }
                            }
                            i++;
                        }
                        prod.VisibleIndividually = true;

                        prod.FullDescription = p.Description + "<p><br /><span style=\"font-size: small;\"><strong>Gwarancja:</strong></span></p><p><span style=\"font-size: small;\">" + p.Guarantee + "</span></p>" + "<p><br /><span style =\"font-size: small;\"><strong>Płeć:</strong></span></p><p><span style=\"font-size: small;\">" + p.Sex + "</span></p>";
                        prod.Price = newPrice(p.Price.ToString(), productsToImport.priceIncrease); //increase price
                        prod.TaxCategoryId = GetTaxCentralaZabawek(p.Vat);  //get tax ID
                        prod.Weight = Decimal.Parse(p.Weight.ToString());
                        prod.StockQuantity = Int32.Parse(p.Stock.ToString());

                        string category = p.Category;
                        categoriesXML = new string[3];
                        categoriesXML = p.categoryMapped;
                        Category categoryMain = null;
                        bool categoryMainExist = false;
                        var categories = _categoryService.GetAllCategories(); //get all categories
                                                                              //map income categories on categories in NOP
                        foreach (var cat in categories)
                        {
                            if (categoriesXML.Length > 0 && cat.Name.Contains(categoriesXML[0]))
                            {
                                categoryMainExist = true;
                                categoryMain = cat;
                            }
                        }
                        //if category not exists, crate new
                        if ((categoriesXML.Length > 0) && !categoryMainExist)
                        {
                            categoryMain = new Category();
                            categoryMain.Name = categoriesXML[0];
                            categoryMain.CreatedOnUtc = DateTime.UtcNow;
                            categoryMain.Published = true;
                            categoryMain.UpdatedOnUtc = DateTime.UtcNow;

                            _categoryService.InsertCategory(categoryMain);
                        }

                        subCategory = null;
                        bool subCategoryExist = false;
                        var subCategories = _categoryService.GetAllCategoriesByParentCategoryId(categoryMain.Id); //get subcategories of main category
                        foreach (var subcat in subCategories)
                        {
                            if (categoriesXML.Length > 1 && subcat.Name.Contains(categoriesXML[1]))
                            {
                                subCategory = new Category();
                                subCategoryExist = true;
                                subCategory = subcat;
                            }
                        }

                        //if there is no subcategory in NOP, create new if needed
                        if (categoriesXML.Length > 0 && categoriesXML.Length > 1 && !subCategoryExist && categoriesXML[1].Trim() != String.Empty)
                        {
                            subCategory = new Category();
                            subCategory.Name = categoriesXML[1];
                            subCategory.Published = true;
                            subCategory.ParentCategoryId = categoryMain.Id;
                            subCategory.CreatedOnUtc = DateTime.UtcNow;
                            subCategory.UpdatedOnUtc = DateTime.UtcNow;

                            _categoryService.InsertCategory(subCategory);
                        }

                        subCategory2 = null;
                        bool subCategoryExist2 = false;
                        if (subCategory != null)
                        {
                            var subCategories2 = _categoryService.GetAllCategoriesByParentCategoryId(subCategory.Id);
                            //get subcategories of sub category
                            foreach (var subcat in subCategories2)
                            {
                                if (categoriesXML.Length > 2 && subcat.Name.Contains(categoriesXML[2]))
                                {
                                    subCategory2 = new Category();
                                    subCategoryExist2 = true;
                                    subCategory2 = subcat;
                                }
                            }
                        }

                        //if there is no subcategory in NOP, create new if needed
                        if (categoriesXML.Length > 0 && categoriesXML.Length > 2 && !subCategoryExist2 && categoriesXML[2].Trim() != String.Empty)
                        {
                            subCategory2 = new Category();
                            subCategory2.Name = categoriesXML[2];
                            subCategory2.Published = true;
                            subCategory2.ParentCategoryId = subCategory.Id;
                            subCategory2.CreatedOnUtc = DateTime.UtcNow;
                            subCategory2.UpdatedOnUtc = DateTime.UtcNow;

                            _categoryService.InsertCategory(subCategory2);
                        }

                        //create manufacturer
                        Manufacturer manufact = null;
                        bool manufacturerExist = false;
                        var Manufactures = _manufacturerService.GetAllManufacturers();
                        foreach (var manu in Manufactures)
                        {
                            if (p.Manufacturer != null && manu.Name.Contains(p.Manufacturer))
                            {
                                manufacturerExist = true;
                                manufact = manu;
                            }
                        }

                        //if there is now Manufacturer in NOP, create new
                        if (!manufacturerExist)
                        {

                            if (p.Manufacturer != null)
                            {
                                manufact = new Manufacturer();
                                manufact.Name = p.Manufacturer;
                                manufact.CreatedOnUtc = DateTime.UtcNow;
                                manufact.Published = true;
                                manufact.UpdatedOnUtc = DateTime.UtcNow;

                                _manufacturerService.InsertManufacturer(manufact);
                            }
                        }

                        //update dates in product
                        //Some Code Here Deleted to put on GitHub - not public

                        //insert product to get it's ID, needed in adding category and manufacturer
                        _productService.InsertProduct(prod);

                        int warehouseCheckedCounter = 0;

                        foreach (var ware in productsToImport.warehousesList)
                        {
                            if (ware.isChecked)
                                warehouseCheckedCounter++;
                        }

                        if (warehouseCheckedCounter == 1)
                        {
                            warehouseChecked war = productsToImport.warehousesList.FirstOrDefault();
                            if (war.isChecked)
                                prod.WarehouseId = war.warehouse.Id;
                        }

                        if (warehouseCheckedCounter > 1)
                        {
                            prod.ManageInventoryMethod = ManageInventoryMethod.ManageStock;
                            prod.UseMultipleWarehouses = true;
                        }

                        if (productsToImport.warehousesList.Count() > 0 && warehouseCheckedCounter > 1)
                        {
                            var warehouses = _shippingService.GetAllWarehouses();

                            foreach (var ware in productsToImport.warehousesList)
                            {
                                if (ware.isChecked)
                                {

                                    pwi = new ProductWarehouseInventory
                                    {
                                        WarehouseId = ware.warehouse.Id,
                                        StockQuantity = 1,
                                        ProductId = prod.Id,
                                        Product = prod,
                                        Warehouse = warehouses.FirstOrDefault(x => x.Id == ware.warehouse.Id)
                                    };
                                    prod.ProductWarehouseInventory.Add(pwi);
                                }
                            }
                        }

                        _productService.UpdateProduct(prod);

                        if (subCategory == null)
                        {
                            subCategory = categoryMain;
                        }

                        //map product to category
                        //Some Code Here Deleted to put on GitHub - not public

                        //map product to manufacturer
                        if (manufact != null)
                        {
                            var productManufacturer = new ProductManufacturer
                            {
                                ProductId = prod.Id,
                                ManufacturerId = manufact.Id,
                                IsFeaturedProduct = false,
                                DisplayOrder = 1
                            };
                            _manufacturerService.InsertProductManufacturer(productManufacturer);
                        }

                        prod.LimitedToStores = productsToImport.isLimitedToStores;
                        if (!productsToImport.isLimitedToStores)
                        {
                            foreach (storeChecked st in productsToImport.shopsAddTo)
                            {
                                _storeMapingService.InsertStoreMapping(prod, st.store.Id);
                            }
                        }
                        else
                        {
                            foreach (storeChecked st in productsToImport.shopsAddTo)
                            {
                                if (st.isChecked)
                                    _storeMapingService.InsertStoreMapping(prod, st.store.Id);
                            }
                        }
                    }

                }
            }
            catch (Exception e) { _logger.Error(e.Message, e); };
        }

        protected decimal newPrice(string node, int pricePerCentInner)
        {
            decimal newPrice = Decimal.Parse(node);
            newPrice = newPrice + (pricePerCentInner * newPrice) / 100; //increase price by per cent
            decimal returnPrice = Math.Round(newPrice, 2);  //round to 2 digits after mark
            return returnPrice;
        }

        private const string _mimeType = "application/octet-stream";

        //get filetype
        protected virtual string GetMimeTypeFromFilePath(string filePath)
        {
            var mimeType = MimeMapping.GetMimeMapping(filePath);

            if (mimeType == _mimeType)
                mimeType = "image/jpeg";

            return mimeType;
        }

        protected int GetTaxCentralaZabawek(string type)
        {
            decimal tax = 0;
            //Maps wholesale values from XML to tax rates
            switch (type)
            {
                case "A": tax = 23; break;
                case "B": tax = 8; break;
                case "C": tax = 0; break;
                case "D": tax = 5; break;
            }

            CalculateTaxRequest ctr = new CalculateTaxRequest();
            ITaxProvider _itp = _taxService.LoadActiveTaxProvider(); //get taxproviders
            TaxCategory tc = null;
            bool taxExist = false;

            //get all tax categories to find tax from XML
            var taxCategoryList = _taxCategoryService.GetAllTaxCategories();
            //find tax in NOP that rate is equal to tax from wholesale's XML
            foreach (var taxCategory in taxCategoryList)
            {
                ctr.TaxCategoryId = taxCategory.Id;
                decimal rate = _itp.GetTaxRate(ctr).TaxRate;
                //if tax in NOP exists, map it into product by tc = taxCategory
                if (rate == tax)
                {
                    tc = taxCategory;
                    taxExist = true;
                    break;
                }
            }
            //if tax category not exists, create new 
            if (!taxExist)
            {
                tc = new TaxCategory();
                var str = _localizationService.GetLocaleStringResourceByName("Plugin.Misc.ImportProducts.Tax.Rate");
                tc.Name = str.ResourceValue + " " + tax.ToString() + " %";
                _taxCategoryService.InsertTaxCategory(tc);

                _settingService.SetSetting(string.Format("Tax.TaxProvider.FixedRate.TaxCategoryId{0}", tc.Id), tax);
            }
            //return of found or created new tax category
            return tc.Id;
        }
        #endregion

        Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.PRODUCTS productsToMotyleBig;
        Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.PRODUCTS productsToMotyleBig2;
        public Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.PRODUCTS LoadFromXMLFileMotyle (string filePath)
        {
            xmlSerializer = new XmlSerializer(typeof(Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.PRODUCTS));

            TextReader txtReader2 = new StreamReader(filePath);
            string parse = txtReader2.ReadToEnd();
            string savePath = AppDomain.CurrentDomain.BaseDirectory.ToString() + "App_Data /uploads/uploadMotyle.xml";
            using (StreamWriter outputFile = new StreamWriter(savePath))
            {
                outputFile.WriteLine(parse);
            }
            txtReader2.Close();
            //Some Code Here Deleted to put on GitHub - not public

            return productsToMotyleBig;
        }

        CookieContainer cookieContainer;
        public Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.PRODUCTS LoadProductsFromXML(importData importaData)
        {
            cookieContainer = new CookieContainer();
            string formUrl = "http://www.motyleksiazkowe.pl/logowanie"; // NOTE: This is the URL the form POSTs to, not the URL of the form (you can find this in the "action" attribute of the HTML's form tag
            string formParams = "email="+importaData.login+"&passwd= "+importaData.password+"&SubmitLogin=";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(formUrl);
            req.ContentType = "application/x-www-form-urlencoded";
            req.Method = "POST";
            req.CookieContainer = cookieContainer; 
            byte[] bytes = Encoding.ASCII.GetBytes(formParams);
            req.ContentLength = bytes.Length;
            using (Stream os = req.GetRequestStream())
            {
                os.Write(bytes, 0, bytes.Length);
            }

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse()) {}

            Uri uri = new Uri("http://www.motyleksiazkowe.pl");
            string getUrl = importaData.link;
            HttpWebRequest getRequest = (HttpWebRequest)WebRequest.Create(getUrl);
            getRequest.CookieContainer = cookieContainer;
            getRequest.Method = "GET";
            getRequest.KeepAlive = true;
            getRequest.Host = "www.motyleksiazkowe.pl";
            getRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            getRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:44.0) Gecko/20100101 Firefox/44.0";

            HttpWebResponse getResponse = (HttpWebResponse)getRequest.GetResponse();
            using (StreamReader sr = new StreamReader(getResponse.GetResponseStream(), Encoding.UTF8))
            {
                xmlSerializer = new XmlSerializer(typeof(Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.PRODUCTS));

                //// DeSerialize from the StreamReader
                TextReader txtReader2 = sr;
                //Some Code Here Deleted to put on GitHub - not public

                txtReader2.Close();
                //Some Code Here Deleted to put on GitHub - not public
            }

            return productsToMotyleBig;
        }

        Category subCategory;
        Category subCategory2;
        ProductWarehouseInventory pwi;
        public void ImportMotyle(Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.PRODUCTS productsToImport)
        {
            Product productMotyle;
            ProductSpecificationAttribute psa;

            int quantity = 1;
            string wymiary;

            foreach (var prod in productsToImport.product)
            {
                productMotyle = new Product();
                if (prod.Name != null && prod.isNeeded)
                {
                    try
                    {
                        Int32.TryParse(prod.Stock, out quantity);
                        productMotyle.Name = prod.Name;
                        
                        productMotyle.ManufacturerPartNumber =prod.Ean;
                        productMotyle.Price = newPrice(prod.Price, productsToImport.priceIncrease); 
                        productMotyle.TaxCategoryId = GetTaxMotyle(prod.Vat);
                        productMotyle.Sku = prod.ISBN;
                        productMotyle.FullDescription = prod.Description;
                        wymiary = prod.Dimensions;
                        if (wymiary != null)
                        {
                            productMotyle.Height = zwrocWymiar(wymiary, true);
                            productMotyle.Width = zwrocWymiar(wymiary, false);
                        }

                        int i = 0;
                        //add images to product from list of images links
                      
                            var mimeType = GetMimeTypeFromFilePath(prod.Url_img); //get type of image

                            //get image from URL on wholesale server
                            HttpWebRequest lxRequest = (HttpWebRequest)WebRequest.Create(prod.Url_img);

                            String lsResponse = string.Empty;
                            using (HttpWebResponse lxResponse = (HttpWebResponse)lxRequest.GetResponse())
                            {
                                using (BinaryReader reader = new BinaryReader(lxResponse.GetResponseStream()))
                                {
                                    // make a binary table where image will be stored to put into database
                                    var newPictureBinary = reader.ReadBytes(1 * 1024 * 1024 * 10);
                                    //insert image into database
                                    var newPicture = _pictureService.InsertPicture(newPictureBinary, mimeType, _pictureService.GetPictureSeName(prod.Name));
                                    //map added picture to dabatese to the product
                                    productMotyle.ProductPictures.Add(new ProductPicture
                                    {
                                        PictureId = newPicture.Id,
                                        DisplayOrder = i,
                                    });
                                }
                            }
                            i++;

                        productMotyle.StockQuantity = quantity;

                        //Some Code Here Deleted to put on GitHub - not public

                        //insert product to get it's ID, needed in adding category and manufacturer
                        _productService.InsertProduct(productMotyle);

                        string category = prod.Categories;
                        categoriesXML = new string[3];
                        categoriesXML = prod.categoryMapped;
                        Category categoryMain = null;
                        bool categoryMainExist = false;
                        var categories = _categoryService.GetAllCategories(); //get all categories

                        foreach (var cat in categories)
                        {
                            if ((categoriesXML.Length > 0 ) && cat.Name.Contains(categoriesXML[0]))
                            {
                                categoryMainExist = true;
                                categoryMain = cat;
                            }
                        }
                        //if category not exists, crate new
                        if ((categoriesXML.Length > 0) && !categoryMainExist)
                        {
                            categoryMain = new Category();
                            categoryMain.Name = categoriesXML[0];
                            categoryMain.CreatedOnUtc = DateTime.UtcNow;
                            categoryMain.Published = true;
                            categoryMain.UpdatedOnUtc = DateTime.UtcNow;

                            _categoryService.InsertCategory(categoryMain);
                        }

                        subCategory = null;
                        bool subCategoryExist = false;
                        var subCategories = _categoryService.GetAllCategoriesByParentCategoryId(categoryMain.Id); //get subcategories of main category
                        foreach (var subcat in subCategories)
                        {
                            if (categoriesXML.Length > 1 && subcat.Name.Contains(categoriesXML[1]))
                            {
                                subCategory = new Category();
                                subCategoryExist = true;
                                subCategory = subcat;
                            }
                        }

                        //if there is no subcategory in NOP, create new if needed
                        if (categoriesXML.Length > 0 && categoriesXML.Length > 1 && !subCategoryExist && categoriesXML[1].Trim() != String.Empty)
                        {
                            subCategory = new Category();
                            subCategory.Name = categoriesXML[1];
                            subCategory.Published = true;
                            subCategory.ParentCategoryId = categoryMain.Id;
                            subCategory.CreatedOnUtc = DateTime.UtcNow;
                            subCategory.UpdatedOnUtc = DateTime.UtcNow;

                            _categoryService.InsertCategory(subCategory);
                        }

                        subCategory2 = null;
                        bool subCategoryExist2 = false;
                        if (subCategory != null)
                        {
                            var subCategories2 = _categoryService.GetAllCategoriesByParentCategoryId(subCategory.Id);
                            //get subcategories of sub category
                            foreach (var subcat in subCategories2)
                            {
                                if (categoriesXML.Length > 2 && subcat.Name.Contains(categoriesXML[2]))
                                {
                                    subCategory2 = new Category();
                                    subCategoryExist2 = true;
                                    subCategory2 = subcat;
                                }
                            }
                        }

                        //if there is no subcategory in NOP, create new if needed
                        if (categoriesXML.Length > 0 && categoriesXML.Length > 2 && !subCategoryExist2 && categoriesXML[2].Trim() != String.Empty)
                        {
                            subCategory2 = new Category();
                            subCategory2.Name = categoriesXML[2];
                            subCategory2.Published = true;
                            subCategory2.ParentCategoryId = subCategory.Id;
                            subCategory2.CreatedOnUtc = DateTime.UtcNow;
                            subCategory2.UpdatedOnUtc = DateTime.UtcNow;

                            _categoryService.InsertCategory(subCategory2);
                        }

                        if (subCategory == null)
                        {
                            subCategory = categoryMain;
                        }

                        //map product to category
                        //Some Code Here Deleted to put on GitHub - not public

                        productMotyle.LimitedToStores = productsToImport.isLimitedToStores;
                        if (!productsToImport.isLimitedToStores)
                        {
                            foreach (storeChecked st in productsToImport.shopsAddTo)
                            {
                                _storeMapingService.InsertStoreMapping(productMotyle, st.store.Id);
                            }
                        }
                        else
                        {
                            foreach (storeChecked st in productsToImport.shopsAddTo)
                            {
                                if (st.isChecked)
                                    _storeMapingService.InsertStoreMapping(productMotyle, st.store.Id);
                            }
                        }

                        productMotyle.ManageInventoryMethod = ManageInventoryMethod.DontManageStock;
                        productMotyle.UseMultipleWarehouses = false;

                        int warehouseCheckedCounter=0;

                        foreach (var ware in productsToImport.warehousesList)
                        {
                            if (ware.isChecked)
                                warehouseCheckedCounter++;
                        }

                        if (warehouseCheckedCounter == 1)
                        {
                            warehouseChecked war = productsToImport.warehousesList.FirstOrDefault();
                            if (war.isChecked)
                                productMotyle.WarehouseId = war.warehouse.Id;
                        }

                        if (warehouseCheckedCounter > 1)
                        {
                            productMotyle.ManageInventoryMethod = ManageInventoryMethod.ManageStock;
                            productMotyle.UseMultipleWarehouses = true;
                        }

                        if (productsToImport.warehousesList.Count() > 0 && warehouseCheckedCounter > 1)
                        {
                            var warehouses = _shippingService.GetAllWarehouses();

                            foreach (var ware in productsToImport.warehousesList)
                            {
                                if (ware.isChecked)
                                {
                                   
                                    pwi = new ProductWarehouseInventory
                                    {
                                        WarehouseId = ware.warehouse.Id,
                                        StockQuantity = 1,
                                        ProductId = productMotyle.Id,
                                        Product = productMotyle,
                                        Warehouse = warehouses.FirstOrDefault(x => x.Id == ware.warehouse.Id)
                                    };
                                    productMotyle.ProductWarehouseInventory.Add(pwi);
                                }
                            }
                        }

                        //Addidng specification Attributes
                        if (prod.Count_pages !=null && prod.Count_pages != string.Empty)
                        {
                            psa = new ProductSpecificationAttribute();
                            psa = GetPSA("Ilość stron", productMotyle.Id);  //zmiana na lokalizacje z install
                            psa.CustomValue = prod.Count_pages;
                            psa.Product = productMotyle;
                            productMotyle.ProductSpecificationAttributes.Add(psa);
                        }

                        foreach (var person in prod.Persons)
                        {
                            if (person !=null && person != String.Empty)
                            {
                                psa = new ProductSpecificationAttribute();
                                psa = GetPSA("Autor", productMotyle.Id);  //zmiana na lokalizacje z install
                                psa.CustomValue = person;
                                psa.Product = productMotyle;
                                productMotyle.ProductSpecificationAttributes.Add(psa);
                            }
                        }
                        foreach (var firm in prod.Firms)
                        {
                            if (firm !=null && firm != String.Empty)
                            {
                                psa = new ProductSpecificationAttribute();
                                psa = GetPSA("Wydawca", productMotyle.Id);  //zmiana na lokalizacje z install
                                psa.CustomValue =firm;
                                psa.Product = productMotyle;
                                productMotyle.ProductSpecificationAttributes.Add(psa);
                            }
                        }

                        if (prod.Original_name!=null && prod.Original_name != string.Empty)
                        {
                            psa = new ProductSpecificationAttribute();
                            psa = GetPSA("Oryginalny tytuł", productMotyle.Id);  //zmiana na lokalizacje z install
                            psa.CustomValue = prod.Original_name;
                            psa.Product = productMotyle;
                            productMotyle.ProductSpecificationAttributes.Add(psa);
                        }

                        _productService.UpdateProduct(productMotyle);
                    }
                    catch (Exception ex) { continue; }
                }
            }
        }

        private ProductSpecificationAttribute GetPSA(string atr, int id)
        {
            var saIList = _specificationAttributeService.GetSpecificationAttributes();
            List<SpecificationAttribute> saList = saIList.ToList();
            SpecificationAttribute saLookFor = new SpecificationAttribute();
            foreach (var element in saList)
            {
                if (element.Name == atr)
                    saLookFor = element;
            }

            ProductSpecificationAttribute psa;
            var saoIList = _specificationAttributeService.GetSpecificationAttributeOptionsBySpecificationAttribute(saLookFor.Id);
            List<SpecificationAttributeOption> saoList = saoIList.ToList();
            SpecificationAttributeOption saoLookFor = new SpecificationAttributeOption();
            foreach (var element in saoList)
            {
                if (element.SpecificationAttributeId == saLookFor.Id)
                {
                    saoLookFor = element;
                    psa = new ProductSpecificationAttribute()
                    {
                        AttributeType = SpecificationAttributeType.CustomText,
                        ProductId = id,
                        SpecificationAttributeOptionId = saoLookFor.Id,
                        ShowOnProductPage = true,
                        DisplayOrder = 1
                    };
                    return psa;
                }
            }

            saoLookFor.Name = "Hello";  //any string

            saLookFor = new SpecificationAttribute();
            saLookFor.Name = atr;
            _specificationAttributeService.InsertSpecificationAttribute(saLookFor);
            saoLookFor.SpecificationAttributeId = saLookFor.Id;
            _specificationAttributeService.InsertSpecificationAttributeOption(saoLookFor);

            psa = new ProductSpecificationAttribute()
            {
                AttributeType = SpecificationAttributeType.CustomText,
                ProductId = id,
                SpecificationAttributeOptionId = saoLookFor.Id,
                ShowOnProductPage = true,
                DisplayOrder = 1
            };
            _specificationAttributeService.InsertProductSpecificationAttribute(psa);
            return psa;
        }

        int poczatek;
        int koniec;
        private decimal zwrocWymiar(string v, bool height)
        {
            string work = v.Trim();

            for (int i = 0; i < v.Length; i++)
            {
                if (Int32.TryParse(v[i].ToString(), out poczatek) == true)
                {
                    break;
                }
            }
            for (int i = poczatek; i < v.Length; i++)
            {
                if (v[i] == 'm')
                {
                    if (v[i + 1] == 'm')
                    {
                        koniec = i;
                        break;
                    }
                }
            }

            string nowa = work.Substring(poczatek, koniec);

            string[] wynik;
            wynik = work.Split('x');
            decimal doZwrotu = 0;


            if (height == true)
                decimal.TryParse(wynik[1], out doZwrotu);
            else
                decimal.TryParse(wynik[0], out doZwrotu);
            return doZwrotu;
        }

        protected int GetTaxMotyle(string type)
        {
            decimal tax = 0;
            //Maps wholesale values from XML to tax rates
            switch (type)
            {
                case "23": tax = 23; break;
                case "8": tax = 8; break;
                case "0": tax = 0; break;
                case "5": tax = 5; break;
            }

            CalculateTaxRequest ctr = new CalculateTaxRequest();
            ITaxProvider _itp = _taxService.LoadActiveTaxProvider(); //get taxproviders
            TaxCategory tc = null;
            bool taxExist = false;

            //get all tax categories to find tax from XML
            var taxCategoryList = _taxCategoryService.GetAllTaxCategories();
            //find tax in NOP that rate is equal to tax from wholesale's XML
            foreach (var taxCategory in taxCategoryList)
            {
                ctr.TaxCategoryId = taxCategory.Id;
                decimal rate = _itp.GetTaxRate(ctr).TaxRate;
                //if tax in NOP exists, map it into product by tc = taxCategory
                if (rate == tax)
                {
                    tc = taxCategory;
                    taxExist = true;
                    break;
                }
            }
            //if tax category not exists, create new 
            if (!taxExist)
            {
                tc = new TaxCategory();
                var str = _localizationService.GetLocaleStringResourceByName("Plugin.Misc.ImportProducts.Tax.Rate");
                tc.Name = str.ResourceValue + " " + tax.ToString() + " %";
                _taxCategoryService.InsertTaxCategory(tc);

                _settingService.SetSetting(string.Format("Tax.TaxProvider.FixedRate.TaxCategoryId{0}", tc.Id), tax);
            }
            //return of found or created new tax category
            return tc.Id;
        }
    }
}
