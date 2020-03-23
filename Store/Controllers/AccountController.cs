using Store.Models.Data;
using Store.Models.ViewModels.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Store.Controllers
{
    public class AccountController : Controller
    {
        // GET : Account
        public ActionResult Index()
        {
            return RedirectToAction("Login");
        }

        // GET : account/create-account
        [ActionName("create-account")]
        [HttpGet]
        public ActionResult CreateAccount()
        {

            return View("CreateAccount");
        }

        // POST: account/create-account
        [ActionName("create-account")]
        [HttpPost]
        public ActionResult CreateAccount(UserVM model)
        {
            if (!ModelState.IsValid)
            {
                return View("CreateAccount", model);
            }

            if (!model.Password.Equals(model.ConfirmPassword))
            {
                ModelState.AddModelError("", "Password do not match!");
                return View("CreateAccount", model);
            }

            using (Db db = new Db())
            {
                if (db.Users.Any(x => x.Username.Equals(model.Username)))
                {
                    ModelState.AddModelError("", $"Username {model.Username}is taken.");
                    model.Username = "";
                    return View("CreateAccount", model);
                }
                UserDTO userDTO = new UserDTO()
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailAddress = model.EmailAddress,
                    Username = model.Username,
                    Password = model.Password
                };
                db.Users.Add(userDTO);
                db.SaveChanges();
                int id = userDTO.Id;
                UserRoleDTO userRoleDTO = new UserRoleDTO()
                {
                    UserId = id,
                    RoleId = 2 //Роль обычного пользователя - 2 , админа - 1
                };
                db.UserRoles.Add(userRoleDTO);
                db.SaveChanges();
            }
            TempData["SM"] = "You are now registered and can login.";

            return RedirectToAction("Login");
        }

        // GET: Account/Login
        [HttpGet]
        public ActionResult Login()
        {
            string userName = User.Identity.Name;
            if (!string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("user-profile");
            }
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        public ActionResult Login(LoginUserVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            bool isValid = false;
            using (Db db = new Db())
            {
                if (db.Users.Any(x => x.Username.Equals(model.Username) && x.Password.Equals(model.Password)))
                {
                    isValid = true;
                }
                if (!isValid)
                {
                    ModelState.AddModelError("", "Invalid username or password.");
                    return View(model);
                }
                else
                {
                    FormsAuthentication.SetAuthCookie(model.Username, model.RememberMe);
                    int id = db.Users.Where(x => x.Username.Equals(model.Username)).Select (x => x.Id).FirstOrDefault();
                    if (db.UserRoles.Where(x => x.UserId == id).Select(x => x.RoleId).FirstOrDefault() == 1)
                    {
                        return Redirect("/Admin/Pages/Index");
                    }
                    else
                    {
                        return Redirect(FormsAuthentication.GetRedirectUrl(model.Username, model.RememberMe)); // Переадресация на defaultUrl из Web.config
                    }
                }

            }
        }
        //GET: /account/logout
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        public PartialViewResult Authentic()
        {
            return HttpContext.User.Identity.IsAuthenticated ? PartialView("_Authentic") : PartialView("_LoginRegister");
        }
    }
}