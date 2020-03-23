using Store.Models.Data;
using Store.Models.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Store.Controllers
{
    public class PagesController : Controller
    {
        // GET: Index/{page}
        public ActionResult Index(string page = "")
        {
            //получаем/устанавливаем  краткий заголовок  (Slug)
            if (page == "")
            {
                page = "home";
            }
            PageVM model;
            PagesDTO dto;
            using (Db db = new Db())
            {
                if (!db.Pages.Any(x => x.Slug.Equals(page)))
                {
                    return RedirectToAction("Index", new { page = ""}); // Если страница не найдена => переадресация на главную страницу
                }
            }

            using (Db db = new Db())
            {
                dto = db.Pages.Where(x => x.Slug == page).FirstOrDefault();
            }
            ViewBag.PageTitle = dto.Title;

            //Проверяем наличие боковой панели
            if (dto.HasSidebar)
            {
                ViewBag.Sidebar = "Yes";
            }
            else
            {
                ViewBag.Sidebar = "No";
            }
            model = new PageVM(dto);

                return View(model);
        }


        // Partial View
        public ActionResult PagesMenuPartial()
        {
            List<PageVM> pageVMList;

            using (Db db = new Db())
            {
                pageVMList = db.Pages.ToArray().OrderBy(x => x.Sorting).Where(x => x.Slug != "home")
                    .Select(x => new PageVM(x)).ToList();
            }
            return PartialView("_PagesMenuPartial", pageVMList);
        }

        public ActionResult SidebarPartial()
        {
            SidebarVM model;

            using (Db db = new Db())
            {
                SidebarDTO dto = db.Sidebars.Find(1);

                model = new SidebarVM(dto);
            }

                return PartialView("_SidebarPartial", model);
        }
    }
}