using PagedList;
using Store.Models.Data;
using Store.Models.ViewModels.Shop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace Store.Areas.Admin.Controllers
{
    public class ShopController : Controller
    {
        // GET: Admin/Shop
        public ActionResult Categories()
        {
            List<CategoryVM> categoryVMList;
            using (Db db = new Db())
            {
                categoryVMList = db.Categories.ToArray().OrderBy(x => x.Sorting).Select(x => new CategoryVM(x)).ToList();
            }
            return View(categoryVMList);
        }

        // POST: Admin/Shop/AddNewCategory
        [HttpPost]
        public string AddNewCategory(string catName)
        {
            string id;

            using (Db db = new Db())
            {
                if (db.Categories.Any(x => x.Name == catName))
                {
                    return "titletaken";
                }
                CategoryDTO dto = new CategoryDTO();
                dto.Name = catName;
                dto.Slug = catName.Replace(" ", "-").ToLower();
                dto.Sorting = int.MaxValue;

                db.Categories.Add(dto);
                db.SaveChanges();

                id = dto.Id.ToString();
            }
            return id;
        }

        // POST: Admin/Shop/ReorderCategories
        [HttpPost]
        public void ReorderCategories(int[] id)
        {
            using (Db db = new Db())
            {
                //Реализуем начальный счетчик
                int count = 1; //т.к. по умолчанию у home 0

                //Инициализируем модель данных
                CategoryDTO dto;

                //Устанавливаем сортировку для каждой страницы
                foreach (var catId in id)
                {
                    dto = db.Categories.Find(catId);
                    dto.Sorting = count;
                    db.SaveChanges();
                    count++;
                }
            }
        }

        // Get: Admin/Shop/DeleteCategory/id
        [HttpGet]
        public ActionResult DeleteCategory(int id)
        {
            using (Db db = new Db())
            {
                //Получение модели категории
                CategoryDTO dto = db.Categories.Find(id);

                //Удаляем категорию
                db.Categories.Remove(dto);
                List<ProductDTO> products = new List<ProductDTO>();
                products = db.Products.Where(x => x.CategoryId == id).ToList();
                foreach (var item in products)
                {
                    item.CategoryId = 0;
                    item.CategoryName = "Unknown Category";
                }
                //Сохраняем изменения в БД
                db.SaveChanges();
            }
            //Добавляем сообщение об успешном удалении

            TempData["SM"] = "You have deleted a page.";

            //Переадресовываем пользователя
            return RedirectToAction("Categories");
        }

        // POST: Admin/Shop/DeleteCategory/id
        [HttpPost]
        public string RenameCategory(string newCatName, int id)
        {
            using (Db db = new Db())
            {
                if (db.Categories.Any(x => x.Name == newCatName))
                {
                    return "titletaken";
                }
                CategoryDTO dto = db.Categories.Find(id);
                dto.Name = newCatName;
                dto.Slug = newCatName.Replace(" ", "-").ToLower();
                db.SaveChanges();
            }
            return "success!";
        }

        // GET: Admin/Shop/AddProduct
        [HttpGet]
        public ActionResult AddProduct()
        {
            ProductVM model = new ProductVM();
            using (Db db = new Db())
            {
                model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
            }
            return View(model);
        }

        // POST: Admin/Shop/AddProduct
        [HttpPost]
        public ActionResult AddProduct(ProductVM model, HttpPostedFileBase file)
        {
            if (!ModelState.IsValid)
            {
                using (Db db = new Db())
                {
                    model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");//Необходимо, чтобы model.Categories было заполнено(иначе Exception)
                    return View(model);
                }
            }
            using (Db db = new Db())
            {
                if (db.Products.Any(x => x.Name == model.Name))
                {
                    model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");//Необходимо, чтобы model.Categories было заполнено(иначе Exception)
                    ModelState.AddModelError("", "This product name is taken!");
                    return View(model);
                }
            }
            int id;
            using (Db db = new Db())
            {
                ProductDTO product = new ProductDTO();
                product.Name = model.Name;
                product.Slug = model.Name.Replace(" ", "-").ToLower();
                product.Description = model.Description;
                product.Price = model.Price;
                product.CategoryId = model.CategoryId;

                CategoryDTO catDTO = db.Categories.FirstOrDefault(x => x.Id == model.CategoryId);
                if (catDTO.Name == null)
                {
                    product.CategoryName = "Unknown Category";
                }
                else
                {
                    product.CategoryName = catDTO.Name;
                }

                db.Products.Add(product);
                db.SaveChanges();

                id = product.Id;
            }
            #region Upload Image
            //Создаем необходимые ссылки директорий
            var originalDirectory = new DirectoryInfo(string.Format($"{Server.MapPath(@"\")}Images\\Uploads"));

            var pathProducts = Path.Combine(originalDirectory.ToString(), "Products");
            var pathProductsId = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());
            var pathProductsIdThumbs = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Thumbs");//Уменьшенные копии image
            var pathProductsIdGallery = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery");
            var pathProductsIdGalleryThumbs = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery\\Thumbs");

            //Проверяем наличие директорий (если нет,то создаем)
            if (!Directory.Exists(pathProducts))
                Directory.CreateDirectory(pathProducts);

            if (!Directory.Exists(pathProductsId))
                Directory.CreateDirectory(pathProductsId);

            if (!Directory.Exists(pathProductsIdThumbs))
                Directory.CreateDirectory(pathProductsIdThumbs);

            if (!Directory.Exists(pathProductsIdGallery))
                Directory.CreateDirectory(pathProductsIdGallery);

            if (!Directory.Exists(pathProductsIdGalleryThumbs))
                Directory.CreateDirectory(pathProductsIdGalleryThumbs);

            //Проверяем, был ли файл загружен
            if (file != null && file.ContentLength > 0)
            {
                //Получаем расширение файла
                string ext = file.ContentType.ToLower();

                //Проверяем расширение файла
                if (ext != "image/jpg" &&
                   ext != "image/jpeg" &&
                   ext != "image/pjpeg" &&
                   ext != "image/gif" &&
                   ext != "image/x-png" &&
                   ext != "image/png")
                {
                    using (Db db = new Db())
                    {
                        model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");//Необходимо, чтобы model.Categories было заполнено(иначе Exception)
                        ModelState.AddModelError("", "The image was not uploaded - wrong image extension!");
                        return View(model);
                    }
                }
                string imageName = file.FileName;

                using (Db db = new Db())
                {
                    ProductDTO dto = db.Products.Find(id);
                    dto.ImageName = imageName;
                    db.SaveChanges();
                }

                //Пути к оригинальному и уменьшенному изображению
                var path = string.Format($"{pathProductsId}\\{imageName}");
                var pathThumbs = string.Format($"{pathProductsIdThumbs}\\{imageName}");

                //Сохраняем оригинальное изображение
                file.SaveAs(path);

                //Сохраняем уменьшенную копию
                WebImage img = new WebImage(file.InputStream);
                img.Resize(200,200).Crop(1,1);
                img.Save(pathThumbs);
            }
            #endregion
            TempData["SM"] = "You have added a product!";
            return RedirectToAction("AddProduct");
        }

        //Метод списка товаров
        // GET: Admin/Shop/Products
        [HttpGet]
        public ActionResult Products(int? page, int? categoryId)//параметры для плагина PagedList.mvc
        {
            List<ProductVM> listOfProductVM;
            var pageNumber = page ?? 1; //Если в page null, то pageNumber = 1 

            using (Db db = new Db())
            {
                listOfProductVM = db.Products.ToArray()
                    .Where(x => categoryId == null || categoryId == 0 || x.CategoryId == categoryId)
                    .Select(x => new ProductVM(x))
                    .ToList();
                ViewBag.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                ViewBag.SelectedCategory = categoryId.ToString();
            }

            var onePageOfProducts = listOfProductVM.ToPagedList(pageNumber, 3);
            ViewBag.onePageOfProducts = onePageOfProducts;

            return View(listOfProductVM);
        }

        // Метод редактирования товаров
        // GET: Admin/Shop/EditProduct
        [HttpGet]
        public ActionResult EditProduct(int id)
        {
            ProductVM model;

            using (Db db = new Db())
            {
                ProductDTO dto = db.Products.Find(id);
                if(dto == null)
                {
                    return Content("That product does not exist.");
                }
                model = new ProductVM(dto);
                model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");

                //Получаем все изображения из галереи
                model.GalleryImages = Directory
                    .EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
                    .Select(fn => Path.GetFileName(fn));
            }
                return View(model);
        }

        // Метод редактирования товаров
        // POST: Admin/Shop/EditProduct
        [HttpPost]
        public ActionResult EditProduct(ProductVM model, HttpPostedFileBase file)
        {
            int id = model.Id;
            using (Db db = new Db())
            {
                model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
            }

            model.GalleryImages = Directory
                   .EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
                   .Select(fn => Path.GetFileName(fn));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (Db db = new Db())
            {
                if(db.Products.Where(x => x.Id != id).Any(x => x.Name == model.Name))
                {
                    ModelState.AddModelError("", "That product name is taken!");
                    return View(model);
                }
            }

            using (Db db = new Db())
            {
                ProductDTO dto = db.Products.Find(id);
                dto.Name = model.Name;
                dto.Slug = model.Name.Replace(" ", "-").ToLower();
                dto.Description = model.Description;
                dto.Price = model.Price;
                dto.CategoryId = model.CategoryId;
                dto.ImageName = model.ImageName;

                CategoryDTO categoryDTO = db.Categories.FirstOrDefault(x => x.Id == model.CategoryId);
                if (categoryDTO == null)
                {
                    dto.CategoryName = "Unknown Category";
                }
                else
                {
                    dto.CategoryName = categoryDTO.Name;
                }
                db.SaveChanges();
            }
            TempData["SM"] = "You have edited the product";
            //логика обработки изображений
            #region Image Upload
            if (file != null && file.ContentLength > 0)
            {
                string ext = file.ContentType.ToLower();//расширение файла

                //Проверяем расширение файла
                if (ext != "image/jpg" &&
                   ext != "image/jpeg" &&
                   ext != "image/pjpeg" &&
                   ext != "image/gif" &&
                   ext != "image/x-png" &&
                   ext != "image/png")
                {
                    using (Db db = new Db())
                    {
                        ModelState.AddModelError("", "The image was not uploaded - wrong image extension!");
                        return View(model);
                    }
                }
                //Создаем необходимые ссылки директорий
                var originalDirectory = new DirectoryInfo(string.Format($"{Server.MapPath(@"\")}Images\\Uploads"));

                var pathProductsId = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());
                var pathProductsIdThumbs = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Thumbs");//Уменьшенные копии image

                //Удаляем существующие файлы из директории
                DirectoryInfo di1 = new DirectoryInfo(pathProductsId);
                DirectoryInfo di2 = new DirectoryInfo(pathProductsIdThumbs);

                foreach (var f in di1.GetFiles())
                {
                    f.Delete();
                }
                foreach (var f in di2.GetFiles())
                {
                    f.Delete();
                }

                string imageName = file.FileName;

                using (Db db = new Db())
                {
                    ProductDTO dto = db.Products.Find(id);
                    dto.ImageName = imageName;
                    db.SaveChanges();
                }
                //Пути к оригинальному и уменьшенному изображению
                var path = string.Format($"{pathProductsId}\\{imageName}");
                var pathThumbs = string.Format($"{pathProductsIdThumbs}\\{imageName}");

                //Сохраняем оригинальное изображение
                file.SaveAs(path);

                //Сохраняем уменьшенную копию
                WebImage img = new WebImage(file.InputStream);
                img.Resize(200, 200).Crop(1,1);
                img.Save(pathThumbs);
            }
            #endregion
            return RedirectToAction("EditProduct");
        }

        // Метод удаления товаров
        // GET: Admin/Shop/DeleteProduct
        [HttpGet]
        public ActionResult DeleteProduct(int id)
        {
            using (Db db = new Db())
            {
                ProductDTO dto = db.Products.Find(id);
                db.Products.Remove(dto);
                db.SaveChanges();
            }

            //Удаляем директории товара
            var originalDirectory = new DirectoryInfo(string.Format($"{Server.MapPath(@"\")}Images\\Uploads"));
            var pathProductsId = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());

            if (Directory.Exists(pathProductsId))
            {
                Directory.Delete(pathProductsId, true);
            }
            return RedirectToAction("Products");
        }

        // Метод сохранения изображений в Gallery
        // POST: Admin/Shop/SaveGalleryImages
        [HttpPost]
        public void SaveGalleryImages(int id)
        {
            foreach (string filename in Request.Files)
            {
                HttpPostedFileBase file = Request.Files[filename];
                if (file != null && file.ContentLength > 0)
                {
                    var originalDirectory = new DirectoryInfo(string.Format($"{Server.MapPath(@"\")}Images\\Uploads"));
                    string pathString = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery");
                    string pathStringThumbs = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery\\Thumbs");
                    var path = string.Format($"{pathString}\\{file.FileName}");
                    var pathThumbs = string.Format($"{pathStringThumbs}\\{file.FileName}");
                    //Сохраняем оригинальные изображения и уменьшеныые копии
                    file.SaveAs(path);
                    WebImage img = new WebImage(file.InputStream);
                    img.Resize(200, 200).Crop(1,1);
                    img.Save(pathThumbs);

                }
            }
        }

        // Метод удаления изображений из Gallery
        // POST: Admin/Shop/DeleteImage/id/imageName
        [HttpPost]
        public void DeleteImage (int id, string imageName)
        {
            string fullpath = Request.MapPath("~/Images/Uploads/Products/" + id.ToString() + "/Gallery/" + imageName);
            string fullpathThumbs = Request.MapPath("~/Images/Uploads/Products/" + id.ToString() + "/Gallery/Thumbs/" + imageName);
            if (System.IO.File.Exists(fullpath))
                System.IO.File.Delete(fullpath);

            if (System.IO.File.Exists(fullpathThumbs))
                System.IO.File.Delete(fullpathThumbs);
        }
    }
}