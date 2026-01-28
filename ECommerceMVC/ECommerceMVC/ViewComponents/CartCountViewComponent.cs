using ECommerceMVC.Helpers;
using ECommerceMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceMVC.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        private readonly CartService _cartService;
        private readonly AuthHelper _authHelper;

        public CartCountViewComponent(CartService cartService, AuthHelper authHelper)
        {
            _cartService = cartService;
            _authHelper = authHelper;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userInfo = _authHelper.GetCurrentUser(HttpContext);
            var sessionId = PersistentSessionHelper.GetOrCreatePersistentSessionId(HttpContext);

            var cart = userInfo != null
                ? await _cartService.GetCartFromDatabaseAsync(userInfo.Username)
                : await _cartService.GetCartFromDatabaseBySessionAsync(sessionId);

            var count = cart.Count;
            return View(count);
        }
    }
}



