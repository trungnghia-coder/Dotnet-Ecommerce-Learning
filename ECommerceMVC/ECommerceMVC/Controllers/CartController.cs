using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.Services;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceMVC.Controllers
{
    public class CartController : Controller
    {
        private readonly Hshop2023Context _context;
        private readonly CartService _cartService;
        private readonly ILogger<CartController> _logger;
        private readonly AuthHelper _authHelper;

        public CartController(Hshop2023Context context, CartService cartService, ILogger<CartController> logger, AuthHelper authHelper)
        {
            _context = context;
            _cartService = cartService;
            _logger = logger;
            _authHelper = authHelper;
        }

        public async Task<IActionResult> Index()
        {
            var userInfo = _authHelper.GetCurrentUser(HttpContext);
            var sessionId = PersistentSessionHelper.GetOrCreatePersistentSessionId(HttpContext);

            List<CartItemVM> cart;

            if (userInfo != null)
            {
                // Logged in user: Get from database
                cart = await _cartService.GetCartFromDatabaseAsync(userInfo.Username);
            }
            else
            {
                // Anonymous user: Get from database by persistent session ID
                cart = await _cartService.GetCartFromDatabaseBySessionAsync(sessionId);
            }

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int id, int quantity = 1)
        {
            var merchandise = _context.HangHoas.Find(id);
            if (merchandise == null)
            {
                return NotFound();
            }

            var userInfo = _authHelper.GetCurrentUser(HttpContext);
            var sessionId = PersistentSessionHelper.GetOrCreatePersistentSessionId(HttpContext);

            if (userInfo != null)
            {
                // Logged in user: Save to database
                var success = await _cartService.AddOrUpdateCartItemAsync(userInfo.Username, sessionId, id, quantity);
                
                if (success)
                {
                    var cart = await _cartService.GetCartFromDatabaseAsync(userInfo.Username);
                    return Json(new { success = true, cartCount = cart.Count });
                }
                
                return Json(new { success = false, message = "Error adding to cart" });
            }
            else
            {
                // Anonymous user: Save to database with persistent session ID
                var success = await _cartService.AddOrUpdateCartItemAsync(null, sessionId, id, quantity);
                
                if (success)
                {
                    var cart = await _cartService.GetCartFromDatabaseBySessionAsync(sessionId);
                    return Json(new { success = true, cartCount = cart.Count });
                }
                
                return Json(new { success = false, message = "Error adding to cart" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            var userInfo = _authHelper.GetCurrentUser(HttpContext);
            var sessionId = PersistentSessionHelper.GetOrCreatePersistentSessionId(HttpContext);

            var success = await _cartService.UpdateQuantityAsync(userInfo?.Username, sessionId, id, quantity);

            if (success)
            {
                var cart = userInfo != null
                    ? await _cartService.GetCartFromDatabaseAsync(userInfo.Username)
                    : await _cartService.GetCartFromDatabaseBySessionAsync(sessionId);

                if (quantity <= 0)
                {
                    return Json(new
                    {
                        success = true,
                        removed = true,
                        subtotal = cart.Sum(x => x.Total),
                        cartCount = cart.Count,
                        isEmpty = !cart.Any()
                    });
                }
                else
                {
                    var item = cart.FirstOrDefault(i => i.MerchandiseId == id);
                    return Json(new
                    {
                        success = true,
                        removed = false,
                        itemTotal = item?.Total ?? 0,
                        subtotal = cart.Sum(x => x.Total),
                        cartCount = cart.Count,
                        quantity = item?.Quantity ?? 0
                    });
                }
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userInfo = _authHelper.GetCurrentUser(HttpContext);
            var sessionId = PersistentSessionHelper.GetOrCreatePersistentSessionId(HttpContext);

            var success = await _cartService.RemoveItemAsync(userInfo?.Username, sessionId, id);

            if (success)
            {
                var cart = userInfo != null
                    ? await _cartService.GetCartFromDatabaseAsync(userInfo.Username)
                    : await _cartService.GetCartFromDatabaseBySessionAsync(sessionId);

                return Json(new
                {
                    success = true,
                    subtotal = cart.Sum(x => x.Total),
                    cartCount = cart.Count,
                    isEmpty = !cart.Any()
                });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            var userInfo = _authHelper.GetCurrentUser(HttpContext);
            var sessionId = PersistentSessionHelper.GetOrCreatePersistentSessionId(HttpContext);

            await _cartService.ClearCartAsync(userInfo?.Username, sessionId);
            
            return RedirectToAction("Index");
        }

        public async Task<int> GetCartCount()
        {
            var userInfo = _authHelper.GetCurrentUser(HttpContext);
            var sessionId = PersistentSessionHelper.GetOrCreatePersistentSessionId(HttpContext);

            var cart = userInfo != null
                ? await _cartService.GetCartFromDatabaseAsync(userInfo.Username)
                : await _cartService.GetCartFromDatabaseBySessionAsync(sessionId);

            return cart.Count;
        }
    }
}
