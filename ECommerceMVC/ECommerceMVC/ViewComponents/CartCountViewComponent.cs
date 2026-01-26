using ECommerceMVC.Helpers;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceMVC.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        private const string CART_KEY = "SHOPPING_CART";

        public IViewComponentResult Invoke()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItemVM>>(CART_KEY) ?? new List<CartItemVM>();
            var count = cart.Count;
            return View(count);
        }
    }
}
