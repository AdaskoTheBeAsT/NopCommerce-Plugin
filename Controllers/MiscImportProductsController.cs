namespace Nop.Plugin.Misc.ImportProducts.Controllers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;

    using Nop.Core.Infrastructure;
    using Nop.Plugin.Misc.ImportProducts.Model;
    using Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek;
    using Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe;
    using Nop.Services.Security;
    using Nop.Services.Shipping;
    using Nop.Services.Stores;
    using Nop.Web.Framework.Controllers;

    [AdminAuthorize]
    public class MiscImportProductsController : BasePluginController
    {
        private readonly IImportService _importService;
        private readonly IStoreService _storeService;
        private readonly IShippingService _shippingService;

        public MiscImportProductsController(IImportService importService, IStoreService storeService, IShippingService shippingService)
        {
            _importService = importService;
            _storeService = storeService;
            _shippingService = shippingService;
        }

        [ChildActionOnly]
        public ActionResult Configure()
        {
            var permissionService = EngineContext.Current.Resolve<IPermissionService>();
            if (!permissionService.Authorize(StandardPermissionProvider.ManageProducts))
            {
                return AccessDeniedView();
            }

            return View("~/Plugins/Misc.ImportProducts/Views/MiscImportProducts/Configure.cshtml");
        }

        public ActionResult Import()
        {
            var permissionService = EngineContext.Current.Resolve<IPermissionService>();
            if (!permissionService.Authorize(StandardPermissionProvider.ManageProducts))
            {
                return AccessDeniedView();
            }

            return View("~/Plugins/Misc.ImportProducts/Views/MiscImportProducts/Import.cshtml");
        }

        [HttpPost]
        public ActionResult PreImport(importData importData, HttpPostedFileBase file)
        {
            var permissionService = EngineContext.Current.Resolve<IPermissionService>();
            if (!permissionService.Authorize(StandardPermissionProvider.ManageProducts))
            {
                return AccessDeniedView();
            }

            if (Request.Files.Count > 0)
            {
                var fileNew = Request.Files[0];
                if (fileNew != null && file.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(fileNew.FileName);
                    var path = Path.Combine(Server.MapPath("~/App_Data/uploads"), fileName);
                    fileNew.SaveAs(path);
                    importData.filePath = path;
                }
            }

            if (importData.targetWholesale == "Centrala Zabawek")
            {
                Products model = null;
                model = PrepareCentralaZabawek(importData);
                return View("~/Plugins/Misc.ImportProducts/Views/MiscImportProducts/PreImportCentralaZabawek.cshtml", model);
            }

            if (importData.targetWholesale == "Motyle")
            {
               var model = PrepareMotyle(importData);
               return View("~/Plugins/Misc.ImportProducts/Views/MiscImportProducts/PreImportMotyle.cshtml", model);
            }

            return View();
        }

        public ActionResult ImportCentralaZabawek(Products productsToImport)
        {
            var permissionService = EngineContext.Current.Resolve<IPermissionService>();
            if (!permissionService.Authorize(StandardPermissionProvider.ManageProducts))
            {
                return AccessDeniedView();
            }

            _importService.ImportCentralaZabawek(productsToImport);
            return View("~/Plugins/Misc.ImportProducts/Views/MiscImportProducts/Import.cshtml");// w przyszlosci zwracaj widok z zaimporotwanymi produktami
        }

        protected ActionResult AccessDeniedView()
        {
            return RedirectToAction("AccessDenied", "Security", new { pageUrl = Request.RawUrl });
        }

        protected Products PrepareCentralaZabawek(importData importData)
        {
            Products model = null;

            if (importData.filePath != null)
            {
                model = _importService.LoadProductFromFileCentralaZabawek(importData.filePath);
            }

            if (importData.link != null)
            {
                model = _importService.LoadProductsCentralaZabawek(importData);
            }

            var storesIList = _storeService.GetAllStores();
            var storeList = storesIList.ToList();
            var storeChekedList = new List<storeChecked>();

            // prepare stores list
            foreach (var store in storeList)
            {
                var st = new storeChecked { store = store, isChecked = false };
                storeChekedList.Add(st);
            }

            // prepare categories to map in View (and couple other properties)
            var mainCategoryMapped = new List<string>();
            var subCategoryMapped = new List<string>();
            subCategoryMapped.Add(string.Empty);
            foreach (var prod in model.product)
            {
                var categoryReplace = prod.Category.Replace(", ", ">");
                prod.CategoryMapped = categoryReplace.Split('>');

                if (prod.CategoryMapped.Length > 0 && !mainCategoryMapped.Contains(prod.CategoryMapped[0])) mainCategoryMapped.Add(prod.CategoryMapped[0]);

                // tu powinien być foreach
                if (prod.CategoryMapped.Length > 1 && !subCategoryMapped.Contains(prod.CategoryMapped[1])) subCategoryMapped.Add(prod.CategoryMapped[1]);
                if (prod.CategoryMapped.Length > 2 && !subCategoryMapped.Contains(prod.CategoryMapped[2])) subCategoryMapped.Add(prod.CategoryMapped[2]);
                prod.IsNeeded = true;
            }

            var warehouses = _shippingService.GetAllWarehouses();
            var warehouseList = warehouses.ToList();
            warehouseChecked whc;
            var warehouseChecked = new List<warehouseChecked>();

            foreach (var wh in warehouseList)
            {
                whc = new warehouseChecked();
                whc.warehouse = wh;
                whc.isChecked = false;

                warehouseChecked.Add(whc);
            }

            model.warehousesList = warehouseChecked;
            model.shopsAddTo = storeChekedList;
            model.isLimitedToStores = false;
            model.mainCategoriesMapped = mainCategoryMapped;
            model.subCategoriesMapped = subCategoryMapped;

            // var pageNumber = page ?? 1;
            // model.pagedProductList = (PagedList<product>) model.product.ToPagedList(pageNumber, 30);
            return model;
        }

        private PRODUCTS PrepareMotyle(importData importData)
        {
            PRODUCTS model = null; // sprawdz czy na pewno w tym wypadku model bedzie typu products, pewnie nie
            if (importData.filePath != null)

                // model = _IImportService.LoadProductsFromXSLFileMotyle(importData.filePath);
                model = _importService.LoadFromXMLFileMotyle(importData.filePath);
            if (importData.link != null)
                model = _importService.LoadProductsFromXML(importData);

            var storesIList = _storeService.GetAllStores();
            var storeList = storesIList.ToList();
            storeChecked st;
            var storeChekedList = new List<storeChecked>();

            // prepare stores list
            foreach (var store in storeList)
            {
                st = new storeChecked();
                st.store = store;
                st.isChecked = false;

                storeChekedList.Add(st);
            }

            // prepare categories to map in View (and couple other properties)
            string categoryReplace;
            var mainCategoryMapped = new List<string>();
            var subCategoryMapped = new List<string>();
            subCategoryMapped.Add(string.Empty);
            foreach (var prod in model.product)
            {
                if (prod != null)
                {
                    prod.categoryMapped = prod.Categories.Split('/');

                    if (prod.categoryMapped.Length > 0 && !mainCategoryMapped.Contains(prod.categoryMapped[0])) mainCategoryMapped.Add(prod.categoryMapped[0]);

                    // tu powinien być foreach
                    if (prod.categoryMapped.Length > 1 && !subCategoryMapped.Contains(prod.categoryMapped[1])) subCategoryMapped.Add(prod.categoryMapped[1]);
                    if (prod.categoryMapped.Length > 2 && !subCategoryMapped.Contains(prod.categoryMapped[2])) subCategoryMapped.Add(prod.categoryMapped[2]);

                    prod.isNeeded = false;
                }
            }

            var warehouses = _shippingService.GetAllWarehouses();
            var warehouseList = warehouses.ToList();
            warehouseChecked whc;
            var warehouseChecked = new List<warehouseChecked>();

            foreach (var wh in warehouseList)
            {
                whc = new warehouseChecked();
                whc.warehouse = wh;
                whc.isChecked = false;

                warehouseChecked.Add(whc);
            }

            model.warehousesList = warehouseChecked;
            model.shopsAddTo = storeChekedList;
            model.isLimitedToStores = false;
            model.mainCategoriesMapped = mainCategoryMapped;
            model.subCategoriesMapped = subCategoryMapped;

            return model;
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ImportMotyleKsiazkowe(PRODUCTS productsToImport)
        {
            var _permissionService = EngineContext.Current.Resolve<IPermissionService>();
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
            {
                return AccessDeniedView();
            }

            _importService.ImportMotyle(productsToImport);
            return View("~/Plugins/Misc.ImportProducts/Views/MiscImportProducts/Import.cshtml");
        }
    }
}
