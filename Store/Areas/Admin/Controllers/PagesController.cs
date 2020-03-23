using Store.Models.Data;
using Store.Models.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Store.Areas.Admin.Controllers
{
    public class PagesController : Controller
    {
        // GET: Admin/Pages
        public ActionResult Index()
        {
            List<PageVM> pageList;
            using (Db db = new Db())
            {
                pageList = db.Pages.ToArray().OrderBy(x => x.Sorting).Select(x => new PageVM(x)).ToList();
            }
            return View(pageList);
        }

        // GET: Admin/Pages/AddPage
        [HttpGet]
        public ActionResult AddPage()
        {
            return View();
        }

        // Post: Admin/Pages/AddPage
        [HttpPost]
        public ActionResult AddPage(PageVM model)
        {
            //Проверка модели на валидность
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (Db db = new Db())
            {
                //Объявляем переменную для краткого описания(slug)
                string slug;

                //Инициализируем класс  PageDTO
                PagesDTO dto = new PagesDTO();

                //Присваиваем заголовок модели
                dto.Title = model.Title.ToUpper();

                //Проверяем, есть ои описание, если нет, присваиваем его
                if (string.IsNullOrWhiteSpace(model.Slug))
                {
                    slug = model.Title.Replace(" ", "-").ToLower();
                }
                else
                {
                    slug = model.Slug.Replace(" ", "-").ToLower();
                }

                //Убеждаемся, что заголовок и краткое описание уникальны
                if(db.Pages.Any(x => x.Title == model.Title))
                {
                    ModelState.AddModelError("", "That Title already exist");
                    return View(model);
                }
                else if (db.Pages.Any(x => x.Slug == model.Slug))
                {
                    ModelState.AddModelError("", "That Slug already exist");
                    return View(model);
                }

                //Присваиваем оставшиеся значения модели
                dto.Slug = slug;
                dto.Body = model.Body;
                dto.HasSidebar = model.HasSidebar;
                dto.Sorting = int.MaxValue;

                //Сохраняем модель в БД
                db.Pages.Add(dto);
                db.SaveChanges();
            }

            //Передаем сообщение через TempData
            TempData["SM"] = "you have added a new page"; //successful message = SM

            //Переадресовываем пользователя на метод Index
            return RedirectToAction("Index");
        }

        // GET: Admin/Pages/EditPage/id
        [HttpGet]
        public ActionResult EditPage (int id)
        {
            //Объявим модель PageVM
            PageVM model;

            using (Db db = new Db())
            {
                //Получаем данные страницы
                PagesDTO dto = db.Pages.Find(id);

                //Проверяем, доступна ли страница
                if (dto == null)
                {
                    return Content("The page does not exist.");
                }

                //Инициализируем модель данными
                model = new PageVM(dto);
            }

            //Возвращаем модель в представление
            return View(model);
        }

        // Post: Admin/Pages/EditPage
        [HttpPost]
        public ActionResult EditPage(PageVM model)
        {
            //Проверяем модель на валидность
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (Db db = new Db())
            {
                //Получаем id страницы
                int id = model.Id;

                //Объявляем переменную краткого заголовка
                string slug = "home";

                //Получаем страницу по id
                PagesDTO dto = db.Pages.Find(id);

                //Присваиваем название из полученной модели в DTO
                dto.Title = model.Title;

                //Проверяем краткий заголовок и присваиваем его, если необходимо
                if(model.Slug != "home")
                {
                    if(string.IsNullOrWhiteSpace(model.Slug))
                    {
                        slug = model.Title.Replace(" ", "-").ToLower();
                    }
                    else
                    {
                        slug = model.Slug.Replace(" ", "-").ToLower();
                    }
                }

                //Проверяем slug и title на уникальность
                if(db.Pages.Where(x => x.Id != id).Any(x => x.Title == model.Title))
                {
                    ModelState.AddModelError("", "That title already exist.");
                    return View(model);
                }
                else if (db.Pages.Where(x => x.Id != id).Any(x => x.Slug == slug))
                {
                    ModelState.AddModelError("", "That slug already exist.");
                    return View(model);
                }

                //записываем остальные значения в класс DTO
                dto.Slug = slug;
                dto.Body = model.Body;
                dto.HasSidebar = model.HasSidebar;

                //Сохраняем изменения в БД
                db.SaveChanges();
            }

            // Устанавливаем сообщение в TempData
            TempData["SM"] = "you have edited the page.";

            //Переадресация пользователя
            return RedirectToAction("EditPage");
        }

        // Get: Admin/Pages/PageDetails/id
        [HttpGet]
        public ActionResult PageDetails(int id)
        {
            //Объявляем модель PageVM
            PageVM model;

            using (Db db = new Db())
            {
                //Получаем страницу
                PagesDTO dto = db.Pages.Find(id);

                //Подтверждаем, что страница доступна
                if(dto == null)
                {
                    return Content("The page does not exist.");
                }
                //Присваиваем модели информацию из БД
                model = new PageVM(dto);
            }
                //Возвращаем модель в представление
                return View(model);
        }

        // Get: Admin/Pages/DeletePage/id
        [HttpGet]
        public ActionResult DeletePage(int id)
        {
            using (Db db = new Db())
            {
                //Получение страницы
                PagesDTO dto = db.Pages.Find(id);

                //Удаляем страницу
                db.Pages.Remove(dto);

                //Сохраняем изменения в БД
                db.SaveChanges();
            }
            //Добавляем сообщение об успешном удалении

            TempData["SM"] = "You have deleted a page.";

            //Переадресовываем пользователя
            return RedirectToAction("Index");
        }

        //Создаем метод сортировки
        // Get: Admin/Pages/ReorderPages
        [HttpPost]
        public void ReorderPages(int [] id)
        {
            using (Db db = new Db())
            {
                //Реализуем начальный счетчик
                int count = 1; //т.к. по умолчанию у home 0

                //Инициализируем модель данных
                PagesDTO dto;

                //Устанавливаем сортировку для каждой страницы
                foreach (var pageId in id)
                {
                    dto = db.Pages.Find(pageId);
                    dto.Sorting = count;
                    db.SaveChanges();
                    count++;
                }
            }
        }

        // Get: Admin/Pages/EditSidebar
        [HttpGet]
        public ActionResult EditSidebar()
        {
            //Объявляем модель
            SidebarVM model;

            using (Db db = new Db())
            {
                //Получаем данные  из DTO
                SidebarDTO dto = db.Sidebars.Find(1);

                //Заполняем модель данными
                model = new SidebarVM(dto);
            }
            //Вернуть представление с моделью
            return View(model);
        }

        // Post: Admin/Pages/EditSidebar
        [HttpPost]
        public ActionResult EditSidebar(SidebarVM model)
        {
            using (Db db = new Db())
            {
                //Получаем данные из DTO
                SidebarDTO dto = db.Sidebars.Find(1);

                //Присваиваем данные в тело (в свойство  Body)
                dto.Body = model.Body;

                //Сохраняем
                db.SaveChanges();
            }

            //Присваиваем сообщение в TempData
            TempData["SM"] = "You have edited the Sidebar!";

            //Переадресация
            return RedirectToAction("EditSidebar");
        }
    }

}