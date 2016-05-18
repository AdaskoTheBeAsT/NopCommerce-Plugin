namespace Nop.Plugin.Misc.ImportProducts.Controllers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Xml.Serialization;
    using Nop.Core.Domain.Catalog;
    using Nop.Core.Domain.Tax;
    using Nop.Plugin.Misc.ImportProducts.Model;
    using Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek;
    using Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe;
    using Nop.Services.Catalog;
    using Nop.Services.Configuration;
    using Nop.Services.Localization;
    using Nop.Services.Logging;
    using Nop.Services.Media;
    using Nop.Services.Shipping;
    using Nop.Services.Stores;
    using Nop.Services.Tax;

    using Product = Nop.Core.Domain.Catalog.Product;

    public class ImportViewModel : IImportService
    {
        private readonly IPictureService _pictureService;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly ITaxService _taxService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IStoreMappingService _storeMapingService;
        private readonly ILogger _logger;
        private readonly IShippingService _shippingService;
        private readonly IShipmentService _shipmentService;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private Products pcz;
        private XmlSerializer xmlSerializer;
        private string[] categoriesXML;

        public ImportViewModel(IPictureService pictureService, IProductService productService, ICategoryService categoryService, IManufacturerService manufacturerService, ITaxCategoryService taxCategoryService, ITaxService taxService, ISettingService settingService, ILocalizationService localizationService, IStoreMappingService storeMapingService, ILogger logger, IShippingService shippingService, IShipmentService shipmentService, ISpecificationAttributeService specificationAttributeService)
        {
            _pictureService = pictureService;
            _productService = productService;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _taxCategoryService = taxCategoryService;
            _taxService = taxService;
            _settingService = settingService;
            _localizationService = localizationService;
            _storeMapingService = storeMapingService;
            _logger = logger;
            _shippingService = shippingService;
            _shipmentService = shipmentService;
            _specificationAttributeService = specificationAttributeService;
        }

        #region Centrala Zabawek

        public Products LoadProductsCentralaZabawek(importData importData)
        {
            xmlSerializer = new XmlSerializer(typeof(Model.CentralaZabawek.Products));
            string newLink;
            var passwordChanged = importData.link; // string important in change of link (replace)

            if (importData.link.Contains("login_partnera") && importData.link.Contains("haslo_partnera"))
            {
                // prepare link - replace oryginal link with login and password
                newLink = importData.link.Replace("login_partnera", importData.login.ToString());
                passwordChanged = newLink.Replace("haslo_partnera", importData.password.ToString());
            }

            // ready link (URI) to get XML
            var targetUri = new Uri(passwordChanged);

            // Get XML from Webservice
            var lxRequestt = (HttpWebRequest)WebRequest.Create(targetUri);

            using (var lxResponsee = (HttpWebResponse)lxRequestt.GetResponse())
            using (var reader = new StreamReader(lxResponsee.GetResponseStream()))
            {
                // serialize XML from webservice to objects of class products
                pcz = (Model.CentralaZabawek.Products)xmlSerializer.Deserialize(reader);
            }

            return pcz;
        }

        public Products LoadProductFromFileCentralaZabawek(string filePath)
        {
            // XmlDocument doc = new XmlDocument();
            xmlSerializer = new XmlSerializer(typeof(Model.CentralaZabawek.Products));

            var path = AppDomain.CurrentDomain.BaseDirectory.ToString()
                          + "Plugins\\Misc.ImportProducts\\Przykladowe.xml";

            // doc.Load(filePath);
            TextReader txtReader = new StreamReader(filePath);

            //// DeSerialize from the StreamReader
            pcz = (Model.CentralaZabawek.Products)xmlSerializer.Deserialize(txtReader);
            txtReader.Close();

            return pcz;
        }

        public void ImportCentralaZabawek(Products productsToImport)
        {
            try
            {
                Product prod = null;

                // add every product in list of products got from websevice XML
                foreach (var p in productsToImport.product)
                {
                    if (p.IsNeeded)
                    {
                        prod = new Product(); // create new product - it has to create new object
                        prod.Name = p.Name;
                        prod.ManufacturerPartNumber = p.Ean;
                        var i = 0;

                        // add images to product from list of images links
                        foreach (var picturePath in p.picturePaths)
                        {
                            var mimeType = GetMimeTypeFromFilePath(picturePath); // get type of image

                            // get image from URL on wholesale server
                            var lxRequest = (HttpWebRequest)WebRequest.Create(picturePath);

                            var lsResponse = string.Empty;
                            using (var lxResponse = (HttpWebResponse)lxRequest.GetResponse())
                            {
                                using (var reader = new BinaryReader(lxResponse.GetResponseStream()))
                                {
                                    // make a binary table where image will be stored to put into database
                                    var newPictureBinary = reader.ReadBytes(1 * 1024 * 1024 * 10);

                                    // insert image into database
                                    var newPicture = _pictureService.InsertPicture(
                                        newPictureBinary,
                                        mimeType,
                                        _pictureService.GetPictureSeName(prod.Name));

                                    // map added picture to dabatese to the product
                                    prod.ProductPictures.Add(
                                        new ProductPicture { PictureId = newPicture.Id, DisplayOrder = i, });
                                }
                            }

                            i++;
                        }

                        prod.VisibleIndividually = true;

                        prod.FullDescription = p.Description
                                               + "<p><br /><span style=\"font-size: small;\"><strong>Gwarancja:</strong></span></p><p><span style=\"font-size: small;\">"
                                               + p.Guarantee + "</span></p>"
                                               + "<p><br /><span style =\"font-size: small;\"><strong>Płeć:</strong></span></p><p><span style=\"font-size: small;\">"
                                               + p.Sex + "</span></p>";
                        prod.Price = newPrice(p.Price.ToString(), productsToImport.priceIncrease); // increase price
                        prod.TaxCategoryId = GetTaxCentralaZabawek(p.Vat); // get tax ID
                        prod.Weight = decimal.Parse(p.Weight.ToString());
                        prod.StockQuantity = int.Parse(p.Stock.ToString());

                        var category = p.Category;
                        categoriesXML = new string[3];
                        categoriesXML = p.CategoryMapped;
                        Category categoryMain = null;
                        var categoryMainExist = false;
                        var categories = _categoryService.GetAllCategories(); // get all categories

                        // map income categories on categories in NOP
                        foreach (var cat in categories)
                        {
                            if (categoriesXML.Length > 0 && cat.Name.Contains(categoriesXML[0]))
                            {
                                categoryMainExist = true;
                                categoryMain = cat;
                            }
                        }

                        // if category not exists, crate new
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
                        var subCategoryExist = false;
                        var subCategories = _categoryService.GetAllCategoriesByParentCategoryId(categoryMain.Id);

                            // get subcategories of main category
                        foreach (var subcat in subCategories)
                        {
                            if (categoriesXML.Length > 1 && subcat.Name.Contains(categoriesXML[1]))
                            {
                                subCategory = new Category();
                                subCategoryExist = true;
                                subCategory = subcat;
                            }
                        }

                        // if there is no subcategory in NOP, create new if needed
                        if (categoriesXML.Length > 0 && categoriesXML.Length > 1 && !subCategoryExist
                            && categoriesXML[1].Trim() != string.Empty)
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
                        var subCategoryExist2 = false;
                        if (subCategory != null)
                        {
                            var subCategories2 = _categoryService.GetAllCategoriesByParentCategoryId(subCategory.Id);

                            // get subcategories of sub category
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

                        // if there is no subcategory in NOP, create new if needed
                        if (categoriesXML.Length > 0 && categoriesXML.Length > 2 && !subCategoryExist2
                            && categoriesXML[2].Trim() != string.Empty)
                        {
                            subCategory2 = new Category();
                            subCategory2.Name = categoriesXML[2];
                            subCategory2.Published = true;
                            subCategory2.ParentCategoryId = subCategory.Id;
                            subCategory2.CreatedOnUtc = DateTime.UtcNow;
                            subCategory2.UpdatedOnUtc = DateTime.UtcNow;

                            _categoryService.InsertCategory(subCategory2);
                        }

                        // create manufacturer
                        Manufacturer manufact = null;
                        var manufacturerExist = false;
                        var Manufactures = _manufacturerService.GetAllManufacturers();
                        foreach (var manu in Manufactures)
                        {
                            if (p.Manufacturer != null && manu.Name.Contains(p.Manufacturer))
                            {
                                manufacturerExist = true;
                                manufact = manu;
                            }
                        }

                        // if there is now Manufacturer in NOP, create new
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

                        // update dates in product
                        // Some Code Here Deleted to put on GitHub - not public

                        // insert product to get it's ID, needed in adding category and manufacturer
                        _productService.InsertProduct(prod);

                        var warehouseCheckedCounter = 0;

                        foreach (var ware in productsToImport.warehousesList)
                        {
                            if (ware.isChecked) warehouseCheckedCounter++;
                        }

                        if (warehouseCheckedCounter == 1)
                        {
                            var war = productsToImport.warehousesList.FirstOrDefault();
                            if (war.isChecked) prod.WarehouseId = war.warehouse.Id;
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
                                                  Warehouse =
                                                      warehouses.FirstOrDefault(
                                                          x => x.Id == ware.warehouse.Id)
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

                        // map product to category
                        // Some Code Here Deleted to put on GitHub - not public

                        // map product to manufacturer
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
                            foreach (var st in productsToImport.shopsAddTo)
                            {
                                _storeMapingService.InsertStoreMapping(prod, st.store.Id);
                            }
                        }
                        else
                        {
                            foreach (var st in productsToImport.shopsAddTo)
                            {
                                if (st.isChecked) _storeMapingService.InsertStoreMapping(prod, st.store.Id);
                            }
                        }
                    }
                }
            }
            catch (Exception e) { _logger.Error(e.Message, e); };
        }

        protected decimal newPrice(string node, int pricePerCentInner)
        {
            var newPrice = decimal.Parse(node);
            newPrice = newPrice + (pricePerCentInner * newPrice) / 100; // increase price by per cent
            var returnPrice = Math.Round(newPrice, 2);  // round to 2 digits after mark
            return returnPrice;
        }

        private const string _mimeType = "application/octet-stream";

        // get filetype
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

            // Maps wholesale values from XML to tax rates
            switch (type)
            {
                case "A":
                    tax = 23;
                    break;
                case "B":
                    tax = 8;
                    break;
                case "C":
                    tax = 0;
                    break;
                case "D":
                    tax = 5;
                    break;
            }

            var ctr = new CalculateTaxRequest();
            var _itp = _taxService.LoadActiveTaxProvider(); // get taxproviders
            TaxCategory tc = null;
            var taxExist = false;

            // get all tax categories to find tax from XML
            var taxCategoryList = _taxCategoryService.GetAllTaxCategories();

            // find tax in NOP that rate is equal to tax from wholesale's XML
            foreach (var taxCategory in taxCategoryList)
            {
                ctr.TaxCategoryId = taxCategory.Id;
                var rate = _itp.GetTaxRate(ctr).TaxRate;

                // if tax in NOP exists, map it into product by tc = taxCategory
                if (rate == tax)
                {
                    tc = taxCategory;
                    taxExist = true;
                    break;
                }
            }

            // if tax category not exists, create new
            if (!taxExist)
            {
                tc = new TaxCategory();
                var str = _localizationService.GetLocaleStringResourceByName("Plugin.Misc.ImportProducts.Tax.Rate");
                tc.Name = str.ResourceValue + " " + tax.ToString() + " %";
                _taxCategoryService.InsertTaxCategory(tc);

                _settingService.SetSetting(string.Format("Tax.TaxProvider.FixedRate.TaxCategoryId{0}", tc.Id), tax);
            }

            // return of found or created new tax category
            return tc.Id;
        }

        #endregion

        PRODUCTS productsToMotyleBig;
        PRODUCTS productsToMotyleBig2;

        public PRODUCTS LoadFromXMLFileMotyle (string filePath)
        {
            xmlSerializer = new XmlSerializer(typeof(Model.MotyleKsiazkowe.PRODUCTS));

            TextReader txtReader2 = new StreamReader(filePath);
            var parse = txtReader2.ReadToEnd();
            var savePath = AppDomain.CurrentDomain.BaseDirectory.ToString() + "App_Data /uploads/uploadMotyle.xml";
            using (var outputFile = new StreamWriter(savePath))
            {
                outputFile.WriteLine(parse);
            }

            txtReader2.Close();

            // Some Code Here Deleted to put on GitHub - not public
            return productsToMotyleBig;
        }

        CookieContainer cookieContainer;

        public PRODUCTS LoadProductsFromXML(importData importaData)
        {
            cookieContainer = new CookieContainer();
            var formUrl = "http://www.motyleksiazkowe.pl/logowanie"; // NOTE: This is the URL the form POSTs to, not the URL of the form (you can find this in the "action" attribute of the HTML's form tag
            var formParams = "email="+importaData.login+"&passwd= "+importaData.password+"&SubmitLogin=";
            var req = (HttpWebRequest)WebRequest.Create(formUrl);
            req.ContentType = "application/x-www-form-urlencoded";
            req.Method = "POST";
            req.CookieContainer = cookieContainer;
            var bytes = Encoding.ASCII.GetBytes(formParams);
            req.ContentLength = bytes.Length;
            using (var os = req.GetRequestStream())
            {
                os.Write(bytes, 0, bytes.Length);
            }

            using (var resp = (HttpWebResponse)req.GetResponse()) {}

            var uri = new Uri("http://www.motyleksiazkowe.pl");
            var getUrl = importaData.link;
            var getRequest = (HttpWebRequest)WebRequest.Create(getUrl);
            getRequest.CookieContainer = cookieContainer;
            getRequest.Method = "GET";
            getRequest.KeepAlive = true;
            getRequest.Host = "www.motyleksiazkowe.pl";
            getRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            getRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:44.0) Gecko/20100101 Firefox/44.0";

            var getResponse = (HttpWebResponse)getRequest.GetResponse();
            using (var sr = new StreamReader(getResponse.GetResponseStream(), Encoding.UTF8))
            {
                xmlSerializer = new XmlSerializer(typeof(Model.MotyleKsiazkowe.PRODUCTS));

                //// DeSerialize from the StreamReader
                TextReader txtReader2 = sr;

                // Some Code Here Deleted to put on GitHub - not public
                txtReader2.Close();

                // Some Code Here Deleted to put on GitHub - not public
            }

            return productsToMotyleBig;
        }

        Category subCategory;
        Category subCategory2;
        ProductWarehouseInventory pwi;

        public void ImportMotyle(PRODUCTS productsToImport)
        {
            Product productMotyle;
            ProductSpecificationAttribute psa;

            var quantity = 1;
            string wymiary;

            foreach (var prod in productsToImport.product)
            {
                productMotyle = new Product();
                if (prod.Name != null && prod.isNeeded)
                {
                    try
                    {
                        int.TryParse(prod.Stock, out quantity);
                        productMotyle.Name = prod.Name;

                        productMotyle.ManufacturerPartNumber = prod.Ean;
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

                        var i = 0;

                        // add images to product from list of images links
                        var mimeType = GetMimeTypeFromFilePath(prod.Url_img); // get type of image

                        // get image from URL on wholesale server
                        var lxRequest = (HttpWebRequest)WebRequest.Create(prod.Url_img);

                        var lsResponse = string.Empty;
                        using (var lxResponse = (HttpWebResponse)lxRequest.GetResponse())
                        {
                            using (var reader = new BinaryReader(lxResponse.GetResponseStream()))
                            {
                                // make a binary table where image will be stored to put into database
                                var newPictureBinary = reader.ReadBytes(1 * 1024 * 1024 * 10);

                                // insert image into database
                                var newPicture = _pictureService.InsertPicture(
                                    newPictureBinary,
                                    mimeType,
                                    _pictureService.GetPictureSeName(prod.Name));

                                // map added picture to dabatese to the product
                                productMotyle.ProductPictures.Add(
                                    new ProductPicture { PictureId = newPicture.Id, DisplayOrder = i, });
                            }
                        }

                        i++;

                        productMotyle.StockQuantity = quantity;

                        // Some Code Here Deleted to put on GitHub - not public

                        // insert product to get it's ID, needed in adding category and manufacturer
                        _productService.InsertProduct(productMotyle);

                        var category = prod.Categories;
                        categoriesXML = new string[3];
                        categoriesXML = prod.categoryMapped;
                        Category categoryMain = null;
                        var categoryMainExist = false;
                        var categories = _categoryService.GetAllCategories(); // get all categories

                        foreach (var cat in categories)
                        {
                            if ((categoriesXML.Length > 0) && cat.Name.Contains(categoriesXML[0]))
                            {
                                categoryMainExist = true;
                                categoryMain = cat;
                            }
                        }

                        // if category not exists, crate new
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
                        var subCategoryExist = false;
                        var subCategories = _categoryService.GetAllCategoriesByParentCategoryId(categoryMain.Id);

                        // get subcategories of main category
                        foreach (var subcat in subCategories)
                        {
                            if (categoriesXML.Length > 1 && subcat.Name.Contains(categoriesXML[1]))
                            {
                                subCategory = new Category();
                                subCategoryExist = true;
                                subCategory = subcat;
                            }
                        }

                        // if there is no subcategory in NOP, create new if needed
                        if (categoriesXML.Length > 0 && categoriesXML.Length > 1 && !subCategoryExist
                            && categoriesXML[1].Trim() != string.Empty)
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
                        var subCategoryExist2 = false;
                        if (subCategory != null)
                        {
                            var subCategories2 = _categoryService.GetAllCategoriesByParentCategoryId(subCategory.Id);

                            // get subcategories of sub category
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

                        // if there is no subcategory in NOP, create new if needed
                        if (categoriesXML.Length > 0 && categoriesXML.Length > 2 && !subCategoryExist2
                            && categoriesXML[2].Trim() != string.Empty)
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

                        // map product to category
                        // Some Code Here Deleted to put on GitHub - not public
                        productMotyle.LimitedToStores = productsToImport.isLimitedToStores;
                        if (!productsToImport.isLimitedToStores)
                        {
                            foreach (var st in productsToImport.shopsAddTo)
                            {
                                _storeMapingService.InsertStoreMapping(productMotyle, st.store.Id);
                            }
                        }
                        else
                        {
                            foreach (var st in productsToImport.shopsAddTo)
                            {
                                if (st.isChecked) _storeMapingService.InsertStoreMapping(productMotyle, st.store.Id);
                            }
                        }

                        productMotyle.ManageInventoryMethod = ManageInventoryMethod.DontManageStock;
                        productMotyle.UseMultipleWarehouses = false;

                        var warehouseCheckedCounter = 0;

                        foreach (var ware in productsToImport.warehousesList)
                        {
                            if (ware.isChecked) warehouseCheckedCounter++;
                        }

                        if (warehouseCheckedCounter == 1)
                        {
                            var war = productsToImport.warehousesList.FirstOrDefault();
                            if (war.isChecked) productMotyle.WarehouseId = war.warehouse.Id;
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
                                                  Warehouse =
                                                      warehouses.FirstOrDefault(
                                                          x => x.Id == ware.warehouse.Id)
                                              };
                                    productMotyle.ProductWarehouseInventory.Add(pwi);
                                }
                            }
                        }

                        // Addidng specification Attributes
                        if (prod.Count_pages != null && prod.Count_pages != string.Empty)
                        {
                            psa = new ProductSpecificationAttribute();
                            psa = GetPSA("Ilość stron", productMotyle.Id); // zmiana na lokalizacje z install
                            psa.CustomValue = prod.Count_pages;
                            psa.Product = productMotyle;
                            productMotyle.ProductSpecificationAttributes.Add(psa);
                        }

                        foreach (var person in prod.Persons)
                        {
                            if (person != null && person != string.Empty)
                            {
                                psa = new ProductSpecificationAttribute();
                                psa = GetPSA("Autor", productMotyle.Id); // zmiana na lokalizacje z install
                                psa.CustomValue = person;
                                psa.Product = productMotyle;
                                productMotyle.ProductSpecificationAttributes.Add(psa);
                            }
                        }

                        foreach (var firm in prod.Firms)
                        {
                            if (firm != null && firm != string.Empty)
                            {
                                psa = new ProductSpecificationAttribute();
                                psa = GetPSA("Wydawca", productMotyle.Id); // zmiana na lokalizacje z install
                                psa.CustomValue = firm;
                                psa.Product = productMotyle;
                                productMotyle.ProductSpecificationAttributes.Add(psa);
                            }
                        }

                        if (prod.Original_name != null && prod.Original_name != string.Empty)
                        {
                            psa = new ProductSpecificationAttribute();
                            psa = GetPSA("Oryginalny tytuł", productMotyle.Id); // zmiana na lokalizacje z install
                            psa.CustomValue = prod.Original_name;
                            psa.Product = productMotyle;
                            productMotyle.ProductSpecificationAttributes.Add(psa);
                        }

                        _productService.UpdateProduct(productMotyle);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
        }

        private ProductSpecificationAttribute GetPSA(string atr, int id)
        {
            var saIList = _specificationAttributeService.GetSpecificationAttributes();
            var saList = saIList.ToList();
            var saLookFor = new SpecificationAttribute();
            foreach (var element in saList)
            {
                if (element.Name == atr)
                    saLookFor = element;
            }

            ProductSpecificationAttribute psa;
            var saoIList = _specificationAttributeService.GetSpecificationAttributeOptionsBySpecificationAttribute(saLookFor.Id);
            var saoList = saoIList.ToList();
            var saoLookFor = new SpecificationAttributeOption();
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

            saoLookFor.Name = "Hello";  // any string

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
            var work = v.Trim();

            for (var i = 0; i < v.Length; i++)
            {
                if (int.TryParse(v[i].ToString(), out poczatek) == true)
                {
                    break;
                }
            }

            for (var i = poczatek; i < v.Length; i++)
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

            var nowa = work.Substring(poczatek, koniec);

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

            // Maps wholesale values from XML to tax rates
            switch (type)
            {
                case "23":
                    tax = 23;
                    break;
                case "8":
                    tax = 8;
                    break;
                case "0":
                    tax = 0;
                    break;
                case "5":
                    tax = 5;
                    break;
            }

            var ctr = new CalculateTaxRequest();
            var _itp = _taxService.LoadActiveTaxProvider(); // get taxproviders
            TaxCategory tc = null;
            var taxExist = false;

            // get all tax categories to find tax from XML
            var taxCategoryList = _taxCategoryService.GetAllTaxCategories();

            // find tax in NOP that rate is equal to tax from wholesale's XML
            foreach (var taxCategory in taxCategoryList)
            {
                ctr.TaxCategoryId = taxCategory.Id;
                var rate = _itp.GetTaxRate(ctr).TaxRate;

                // if tax in NOP exists, map it into product by tc = taxCategory
                if (rate == tax)
                {
                    tc = taxCategory;
                    taxExist = true;
                    break;
                }
            }

            // if tax category not exists, create new
            if (!taxExist)
            {
                tc = new TaxCategory();
                var str = _localizationService.GetLocaleStringResourceByName("Plugin.Misc.ImportProducts.Tax.Rate");
                tc.Name = str.ResourceValue + " " + tax.ToString() + " %";
                _taxCategoryService.InsertTaxCategory(tc);

                _settingService.SetSetting(string.Format("Tax.TaxProvider.FixedRate.TaxCategoryId{0}", tc.Id), tax);
            }

            // return of found or created new tax category
            return tc.Id;
        }
    }
}
