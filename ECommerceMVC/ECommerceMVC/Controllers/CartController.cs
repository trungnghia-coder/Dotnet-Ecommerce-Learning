using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceMVC.Controllers
{
    public class CartController : Controller
    {
        private readonly Hshop2023Context _context;
        private const string CART_KEY = "SHOPPING_CART";

        public CartController(Hshop2023Context context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItemVM>>(CART_KEY) ?? new List<CartItemVM>();
            return View(cart);
        }

        [HttpPost]
        public IActionResult AddToCart(int id, int quantity = 1)
        {
            var merchandise = _context.HangHoas.Find(id);
            if (merchandise == null)
            {
                return NotFound();
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItemVM>>(CART_KEY) ?? new List<CartItemVM>();

            var existingItem = cart.FirstOrDefault(item => item.MerchandiseId == id);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItemVM
                {
                    MerchandiseId = merchandise.MaHh,
                    Merchandisename = merchandise.TenHh,
                    Image = merchandise.Hinh ?? "",
                    Price = merchandise.DonGia ?? 0,
                    Quantity = quantity
                });
            }

            HttpContext.Session.SetObjectAsJson(CART_KEY, cart);

            return Json(new { success = true, cartCount = cart.Count });
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItemVM>>(CART_KEY);
            if (cart != null)
            {
                var item = cart.FirstOrDefault(i => i.MerchandiseId == id);
                if (item != null)
                {
                    if (quantity <= 0)
                    {
                        cart.Remove(item);
                        HttpContext.Session.SetObjectAsJson(CART_KEY, cart);

                        var subtotalAfterRemove = cart.Sum(x => x.Total);
                        var cartCountAfterRemove = cart.Count;

                        return Json(new
                        {
                            success = true,
                            removed = true,
                            subtotal = subtotalAfterRemove,
                            cartCount = cartCountAfterRemove,
                            isEmpty = !cart.Any()
                        });
                    }
                    else
                    {
                        item.Quantity = quantity;
                        HttpContext.Session.SetObjectAsJson(CART_KEY, cart);

                        var subtotal = cart.Sum(x => x.Total);
                        var cartCount = cart.Count;

                        return Json(new
                        {
                            success = true,
                            removed = false,
                            itemTotal = item.Total,
                            subtotal = subtotal,
                            cartCount = cartCount,
                            quantity = item.Quantity
                        });
                    }
                }
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int id)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItemVM>>(CART_KEY);
            if (cart != null)
            {
                var item = cart.FirstOrDefault(i => i.MerchandiseId == id);
                if (item != null)
                {
                    cart.Remove(item);
                    HttpContext.Session.SetObjectAsJson(CART_KEY, cart);

                    var subtotal = cart.Sum(x => x.Total);
                    var cartCount = cart.Count;

                    return Json(new
                    {
                        success = true,
                        subtotal = subtotal,
                        cartCount = cartCount,
                        isEmpty = !cart.Any()
                    });
                }
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove(CART_KEY);
            return RedirectToAction("Index");
        }

        public int GetCartCount()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItemVM>>(CART_KEY) ?? new List<CartItemVM>();
            return cart.Count;
        }
    }
}
