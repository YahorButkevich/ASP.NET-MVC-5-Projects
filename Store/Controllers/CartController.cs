using Store.Models.Data;
using Store.Models.ViewModels.Cart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Store.Controllers
{
    public class CartController : Controller
    {
        // GET: Cart 
        public ActionResult Index()
        {
            List<CartVM> cart = Session["cart"] as List<CartVM> ?? new List<CartVM>(); // Если Session["cart"] пуста, то создается экземпляр List<CartVM>
            if (cart.Count == 0 || Session["cart"] == null)
            {
                ViewBag.Message = "Your cart is empty";
                return View();
            }
            decimal total = 0m;
            foreach (var item in  cart)
            {
                total += item.Total;
            }
            ViewBag.GrandTotal = total;
            return View(cart);
        }

        public ActionResult CartPartial()
        {
            CartVM model = new CartVM();
            int quantity = 0;
            decimal price = 0m;
            //Проверяем сессию корзины
            if (Session["cart"] != null)
            {
                List<CartVM> list = (List<CartVM>)Session["cart"];
                foreach (CartVM item in list)
                {
                    quantity += item.Quantity;
                    price += item.Quantity * item.Price;
                }
                model.Quantity = quantity;
                model.Price = price;
            }
            else
            {
                model.Quantity = 0;
                model.Price = 0m;
            }

            return PartialView("_CartPartial", model);
        }

        public ActionResult AddToCartPartial(int id)
        {
            List<CartVM> cart = Session["cart"] as List<CartVM> ?? new List<CartVM>();
            CartVM model = new CartVM();

            using (Db db = new Db())
            {
                ProductDTO product = db.Products.Find(id);
                var productInCart = cart.FirstOrDefault(x => x.ProductId == id);
                if (productInCart == null)
                {
                    cart.Add(new CartVM()
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = 1,
                        Price = product.Price,
                        Image = product.ImageName
                    });
                }
                else
                {
                    productInCart.Quantity++;
                }
            }
            int quantity = 0;
            decimal price = 0m;

            foreach (var item in cart)
            {
                quantity += item.Quantity;
                price += item.Quantity * item.Price;
            }

            model.Quantity = quantity;
            model.Price = price;
            //Сохраняем состояние корзины в сессию
            Session["cart"] = cart;
            return PartialView("_AddToCartPartial", model);
        }

        //GET: /cart/IncrementProduct
        public JsonResult IncrementProduct(int productId)
        {
            List<CartVM> cart = Session["cart"] as List<CartVM>;

            using (Db db = new Db())
            {
                CartVM model = cart.FirstOrDefault(x => x.ProductId == productId);
                model.Quantity++;
                var result = new { qty = model.Quantity, price = model.Price };
                //Возвращаем Json ответ с данными
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        //GET: /cart/DecrementProduct
        public ActionResult DecrementProduct (int productId)
        {
            List<CartVM> cart = Session["cart"] as List<CartVM>;
            
            using (Db db = new Db())
            {
                CartVM model = cart.FirstOrDefault(x => x.ProductId == productId);
                if (model.Quantity > 1)
                {
                    model.Quantity--;
                }
                else
                {
                    model.Quantity = 0;
                    cart.Remove(model);
                }
                var result = new { qty = model.Quantity, price = model.Price };
                //Возвращаем Json ответ с данными
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        public void RemoveProduct (int productId)
        {
            List<CartVM> cart = Session["cart"] as List<CartVM>;

            using (Db db = new Db())
            {
                CartVM model = cart.FirstOrDefault(x => x.ProductId == productId);
                cart.Remove(model);
            }
        }

        public ActionResult RefreshCart (int productId)
        {
            List<CartVM> cart = Session["cart"] as List<CartVM>;
            decimal price = 0m;
            int qty = 0;
            foreach (var item in cart)
            {
                qty += item.Quantity;
                price += item.Quantity * item.Price;
            }
            CartVM model = new CartVM();
            model.Price = price;
            model.Quantity = qty;
                return PartialView("_AddToCartPartial", model);
        }
    }
}