namespace Nop.Plugin.Misc.ImportProducts.Controllers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using Nop.Core.Domain.Shipping;
    using Nop.Core.Domain.Stores;
    using Nop.Core.Infrastructure;
    using Nop.Plugin.Misc.ImportProducts.Model;
    using Nop.Services.Security;
    using Nop.Services.Shipping;
    using Nop.Services.Stores;
    using Nop.Web.Framework.Controllers;

    [AdminAuthorize]
    public class MiscImportProductsController : BasePluginController
    {
        IImportService _IImportService;
        IStoreService _storeService;
        IShippingService _shippingService;


        public MiscImportProductsController(IImportService IImportService, IStoreService storeService, IShippingService shippingService)
        {
            this._IImportService = IImportService;
            this._storeService = storeService;
            this._shippingService = shippingService;
        }

        [ChildActionOnly]
        public ActionResult Configure()
        {
            var _permissionService = EngineContext.Current.Resolve<IPermissionService>();
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
            {
                return AccessDeniedView();
            }

            return View("~/Plugins/Misc.ImportProducts/Views/MiscImportProducts/Configure.cshtml");
        }

        public ActionResult Import()
        {
            var _permissionService = EngineContext.Current.Resolve<IPermissionService>();
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
            {
                return AccessDeniedView();
            }

            return View("~/Plugins/Misc.ImportProducts/Views/MiscImportProducts/Import.cshtml");
        }

        [HttpPost]
        public ActionResult PreImport(importData importData, HttpPostedFileBase file)
        {
            var _permissionService = EngineContext.Current.Resolve<IPermissionService>();
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
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
                Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.products model = null;
                model = PrepareCentralaZabawek(importData);
                return View("~/Plugins/Misc.ImportProducts/Views/MiscImportProducts/PreImportCentralaZabawek.cshtml", model);
            }

            if (importData.targetWholesale == "Motyle")
            {
               Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.PRODUCTS model = PrepareMotyle(importData);
               return View("~/Plugins/Misc.ImportProducts/Views/MiscImportProducts/PreImportMotyle.cshtml", model);
            }

            return View();
        }

        public ActionResult ImportCentralaZabawek(Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.products productsToImport)
        {
            var _permissionService = EngineContext.Current.Resolve<IPermissionService>();
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
            {
                return AccessDeniedView();
            }

            _IImportService.ImportCentralaZabawek(productsToImport);
            return View("~/Plugins/Misc.ImportProducts/Views/MiscImportProducts/Import.cshtml");//w przyszlosci zwracaj widok z zaimporotwanymi produktami
        }

        protected ActionResult AccessDeniedView()
        {
            return RedirectToAction("AccessDenied", "Security", new { pageUrl = this.Request.RawUrl });
        }

        protected Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.products PrepareCentralaZabawek(importData importData)
        {
            Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.products model = null;

            if (importData.filePath !=null)
                 model = _IImportService.LoadProductFromFileCentralaZabawek(importData.filePath);
            if (importData.link !=null)
                 model = _IImportService.LoadProductsCentralaZabawek(importData);

            var storesIList = _storeService.GetAllStores();
            List<Store> storeList = storesIList.ToList();
            storeChecked st;
            List<storeChecked> storeChekedList = new List<storeChecked>();

            //prepare stores list
            foreach (var store in storeList)
            {
                st = new storeChecked();
                st.store = store;
                st.isChecked = false;

                storeChekedList.Add(st);
            }

            //prepare categories to map in View (and couple other properties)
            string categoryReplace;
            List<string> mainCategoryMapped = new List<string>();
            List<string> subCategoryMapped = new List<string>();
            subCategoryMapped.Add(string.Empty);
            foreach (var prod in model.product)
            {
                categoryReplace = prod.Category.Replace(", ", ">");
                prod.categoryMapped = categoryReplace.Split('>');

                if (prod.categoryMapped.Length > 0 && !mainCategoryMapped.Contains(prod.categoryMapped[0]))
                    mainCategoryMapped.Add(prod.categoryMapped[0]);
                //tu powinien być foreach
                if (prod.categoryMapped.Length > 1 && !subCategoryMapped.Contains(prod.categoryMapped[1]))
                    subCategoryMapped.Add(prod.categoryMapped[1]);
                if (prod.categoryMapped.Length > 2 && !subCategoryMapped.Contains(prod.categoryMapped[2]))
                    subCategoryMapped.Add(prod.categoryMapped[2]);
                prod.isNeeded = true;
            }

            var warehouses = _shippingService.GetAllWarehouses();
            List<Warehouse> warehouseList = warehouses.ToList();
            warehouseChecked whc;
            List<warehouseChecked> warehouseChecked = new List<warehouseChecked>();

            foreach (Warehouse wh in warehouseList)
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

            //var pageNumber = page ?? 1;
            //model.pagedProductList = (PagedList<product>) model.product.ToPagedList(pageNumber, 30);
            return model;
        }

        private Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.PRODUCTS PrepareMotyle(importData importData)
        {
            Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.PRODUCTS model = null; // sprawdz czy na pewno w tym wypadku model bedzie typu products, pewnie nie
            if (importData.filePath != null)
                //model = _IImportService.LoadProductsFromXSLFileMotyle(importData.filePath);
                model = _IImportService.LoadFromXMLFileMotyle(importData.filePath);
            if (importData.link != null)
                model = _IImportService.LoadProductsFromXML(importData);

            var storesIList = _storeService.GetAllStores();
            List<Store> storeList = storesIList.ToList();
            storeChecked st;
            List<storeChecked> storeChekedList = new List<storeChecked>();

            //prepare stores list
            foreach (var store in storeList)
            {
                st = new storeChecked();
                st.store = store;
                st.isChecked = false;

                storeChekedList.Add(st);
            }

            //prepare categories to map in View (and couple other properties)
            string categoryReplace;
            List<string> mainCategoryMapped = new List<string>();
            List<string> subCategoryMapped = new List<string>();
            subCategoryMapped.Add(string.Empty);
            foreach (var prod in model.product)
            {
                if (prod != null)
                {
                    prod.categoryMapped = prod.Categories.Split('/');

                    if (prod.categoryMapped.Length > 0 && !mainCategoryMapped.Contains(prod.categoryMapped[0]))
                        mainCategoryMapped.Add(prod.categoryMapped[0]);
                    //tu powinien być foreach
                    if (prod.categoryMapped.Length > 1 && !subCategoryMapped.Contains(prod.categoryMapped[1]))
                        subCategoryMapped.Add(prod.categoryMapped[1]);
                    if (prod.categoryMapped.Length > 2 && !subCategoryMapped.Contains(prod.categoryMapped[2]))
                        subCategoryMapped.Add(prod.categoryMapped[2]);

                    prod.isNeeded = false;
                }
            }

            var warehouses = _shippingService.GetAllWarehouses();
            List<Warehouse> warehouseList = warehouses.ToList();
            warehouseChecked whc;
            List<warehouseChecked> warehouseChecked = new List<warehouseChecked>();

            foreach (Warehouse wh in warehouseList)
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
        public ActionResult ImportMotyleKsiazkowe(Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.PRODUCTS productsToImport)
        {
            var _permissionService = EngineContext.Current.Resolve<IPermissionService>();
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
            {
                return AccessDeniedView();
            }

            _IImportService.ImportMotyle(productsToImport);
            return View("~/Plugins/Misc.ImportProducts/Views/MiscImportProducts/Import.cshtml");
        }
    }
}
