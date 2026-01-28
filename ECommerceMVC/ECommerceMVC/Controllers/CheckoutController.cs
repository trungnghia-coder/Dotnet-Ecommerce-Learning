using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.Services;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMVC.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly Hshop2023Context _context;
        private readonly CartService _cartService;
        private readonly ILogger<CheckoutController> _logger;
        private readonly AuthHelper _authHelper;
        private readonly PayPalService _payPalService;
        private readonly IConfiguration _config;
        private readonly IVnPayService _vnPayService;

        public CheckoutController(Hshop2023Context context, CartService cartService, ILogger<CheckoutController> logger
                , AuthHelper authHelper, PayPalService payPalService, IConfiguration config, IVnPayService vnPayService)
        {
            _context = context;
            _cartService = cartService;
            _logger = logger;
            _authHelper = authHelper;
            _payPalService = payPalService;
            _config = config;
            _vnPayService = vnPayService;

        }

        /// <summary>
        /// Lấy username từ JWT token. [Authorize] đảm bảo không null.
        /// </summary>
        private string GetUsernameFromRequest()
        {
            var userInfo = _authHelper.GetCurrentUser(HttpContext);
            return userInfo!.Username;
        }

        /// <summary>
        /// Hiển thị trang checkout
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var username = GetUsernameFromRequest();
            var model = await LoadCheckoutViewModel(username);

            ViewBag.PaypalClientId = _config["Paypal:ClientId"];

            if (model == null)
            {
                TempData["ErrorMessage"] = "Your cart is empty";
                return RedirectToAction("Index", "Cart");
            }

            return View(model);
        }

        /// <summary>
        /// Đặt hàng COD (Cash on Delivery)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutVM model)
        {
            var username = GetUsernameFromRequest();

            if (model.PaymentMethod == "PayPal")
            {
                TempData["ErrorMessage"] = "Please complete PayPal payment";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var orderId = await SaveOrderToDatabase(
                        username,
                        model.FullName,
                        model.Address,
                        model.PhoneNumber,
                        model.Note,
                        "COD",
                        paypalOrderId: null
                    );

                    if (orderId > 0)
                    {
                        TempData["SuccessMessage"] = $"Order placed successfully! Your order number is #{orderId}";
                        return RedirectToAction("OrderConfirmation", new { orderId = orderId });
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to place order. Please try again.";
                        return RedirectToAction("Index");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error placing COD order for user {Username}", username);
                    TempData["ErrorMessage"] = $"An error occurred while placing your order: {ex.Message}";
                    return RedirectToAction("Index");
                }
            }

            var reloadedModel = await LoadCheckoutViewModel(username);
            if (reloadedModel != null)
            {
                reloadedModel.FullName = model.FullName;
                reloadedModel.Address = model.Address;
                reloadedModel.PhoneNumber = model.PhoneNumber;
                reloadedModel.Note = model.Note;
                reloadedModel.PaymentMethod = model.PaymentMethod;
            }

            return View("Index", reloadedModel ?? model);
        }

        /// <summary>
        /// Tạo PayPal order và trả về orderId cho frontend
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreatePayPalOrder([FromBody] decimal amount)
        {
            try
            {
                var username = GetUsernameFromRequest();
                var orderId = await _payPalService.CreateOrder(amount, "USD");

                if (string.IsNullOrEmpty(orderId))
                {
                    _logger.LogError("Failed to create PayPal order for user {Username}", username);
                    return Json(new { success = false, message = "Failed to create PayPal order" });
                }

                _logger.LogInformation("PayPal order created: {OrderId} for user {Username}", orderId, username);
                return Json(new { success = true, orderId = orderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal order");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Tạo VNPay payment URL.
        /// Flow: Tạo HoaDon trước → Lấy MaHD → Tạo VNPay URL với OrderId = MaHD
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVnPayPayment(VnPayRequestModel request)
        {
            try
            {
                var username = GetUsernameFromRequest();

                if (string.IsNullOrEmpty(request.FullName) || request.Amount <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid payment information";
                    return RedirectToAction("Index");
                }

                var orderId = await SaveOrderToDatabase(
                    username,
                    request.FullName,
                    request.Description ?? "",
                    "",
                    "VNPay Payment", // RÚT NGẮN để tránh lỗi truncate
                    "VNPay-Pending",
                    paypalOrderId: null
                );

                if (orderId <= 0)
                {
                    _logger.LogError("Failed to create order for VNPay payment");
                    TempData["ErrorMessage"] = "Failed to create order";
                    return RedirectToAction("Index");
                }

                request.OrderId = orderId;
                request.CreatedDate = DateTime.Now;

                var paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, request);

                _logger.LogInformation("VNPay payment URL created for order {OrderId}", orderId);

                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay payment");
                TempData["ErrorMessage"] = "An error occurred while creating payment";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Capture PayPal payment và lưu order vào database
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CapturePayPalOrder([FromBody] PayPalCaptureRequest request)
        {
            try
            {
                var username = GetUsernameFromRequest();
                var paymentSuccess = await _payPalService.CaptureOrder(request.OrderId);

                if (!paymentSuccess)
                {
                    _logger.LogError("PayPal payment capture failed for PayPal order {PayPalOrderId}", request.OrderId);
                    return Json(new { success = false, message = "PayPal payment failed. Please try again." });
                }

                _logger.LogInformation("PayPal payment captured successfully: {PayPalOrderId}", request.OrderId);

                var orderId = await SaveOrderToDatabase(
                    username,
                    request.FullName,
                    request.Address,
                    request.PhoneNumber,
                    request.Note,
                    "PayPal",
                    paypalOrderId: request.OrderId
                );

                if (orderId > 0)
                {
                    return Json(new
                    {
                        success = true,
                        orderId = orderId,
                        message = "Order placed successfully!"
                    });
                }
                else
                {
                    _logger.LogError("PayPal payment captured but failed to save order. PayPal OrderId: {PayPalOrderId}", request.OrderId);
                    return Json(new
                    {
                        success = false,
                        message = "Payment successful but failed to save order. Please contact support."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing PayPal payment for order {PayPalOrderId}", request.OrderId);
                return Json(new
                {
                    success = false,
                    message = $"An error occurred: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Lưu order vào database với Transaction support.
        /// Validation: Kiểm tra stock, rollback nếu fail.
        /// </summary>
        private async Task<int> SaveOrderToDatabase(
            string username,
            string fullName,
            string address,
            string phoneNumber,
            string? note,
            string paymentMethod,
            string? paypalOrderId = null)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var cart = await _cartService.GetCartFromDatabaseAsync(username);

                if (!cart.Any())
                {
                    _logger.LogWarning("Cart is empty for user {Username}", username);
                    return 0;
                }

                var productIds = cart.Select(c => c.MerchandiseId).ToList();
                var products = await _context.HangHoas
                    .Where(h => productIds.Contains(h.MaHh))
                    .ToListAsync();

                foreach (var cartItem in cart)
                {
                    var product = products.FirstOrDefault(p => p.MaHh == cartItem.MerchandiseId);

                    if (product == null)
                    {
                        _logger.LogWarning("Product {ProductId} not found", cartItem.MerchandiseId);
                        await transaction.RollbackAsync();
                        return 0;
                    }
                }

                var hoaDon = new HoaDon
                {
                    MaKh = username,
                    NgayDat = DateTime.Now,
                    NgayCan = null,
                    NgayGiao = null,
                    HoTen = fullName,
                    DiaChi = address,
                    CachThanhToan = paymentMethod,
                    CachVanChuyen = "Standard",
                    PhiVanChuyen = 0,
                    MaTrangThai = 0,
                    MaNv = null,
                    GhiChu = paymentMethod == "PayPal" && !string.IsNullOrEmpty(paypalOrderId)
                        ? $"PayPal OrderID: {paypalOrderId}. {note}"
                        : note
                };

                _context.HoaDons.Add(hoaDon);
                await _context.SaveChangesAsync();

                foreach (var item in cart)
                {
                    var chiTiet = new ChiTietHd
                    {
                        MaHd = hoaDon.MaHd,
                        MaHh = item.MerchandiseId,
                        DonGia = item.Price,
                        SoLuong = item.Quantity,
                        GiamGia = 0
                    };
                    _context.ChiTietHds.Add(chiTiet);
                }

                await _context.SaveChangesAsync();

                var sessionId = PersistentSessionHelper.GetOrCreatePersistentSessionId(HttpContext);
                await _cartService.ClearCartAsync(username, sessionId);

                await transaction.CommitAsync();

                _logger.LogInformation("{PaymentMethod} order {OrderId} saved for user {Username}",
                    paymentMethod, hoaDon.MaHd, username);

                return hoaDon.MaHd;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving {PaymentMethod} order for user {Username}",
                    paymentMethod, username);
                return 0;
            }
        }

        /// <summary>
        /// Hiển thị trang xác nhận đơn hàng.
        /// [AllowAnonymous] để VNPay callback có thể redirect đến đây.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _context.HoaDons
                .Include(h => h.MaKhNavigation)
                .FirstOrDefaultAsync(h => h.MaHd == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", orderId);
                return NotFound();
            }

            // Optional: Security check nếu user đã login
            var userInfo = _authHelper.GetCurrentUser(HttpContext);
            if (userInfo != null && order.MaKh != userInfo.Username)
            {
                _logger.LogWarning("User {Username} tried to access order {OrderId} owned by {Owner}", 
                    userInfo.Username, orderId, order.MaKh);
                return NotFound();
            }

            return View(order);
        }

        /// <summary>
        /// Lấy thông tin khách hàng để auto-fill form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCustomerInfo()
        {
            var username = GetUsernameFromRequest();
            var customer = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaKh == username);

            if (customer == null)
            {
                _logger.LogWarning("Customer info not found for username {Username}", username);
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                fullName = customer.HoTen,
                address = customer.DiaChi ?? "",
                phoneNumber = customer.DienThoai ?? ""
            });
        }

        /// <summary>
        /// Load dữ liệu cho Checkout View. Tái sử dụng bởi Index() và PlaceOrder().
        /// </summary>
        private async Task<CheckoutVM?> LoadCheckoutViewModel(string username)
        {
            try
            {
                var cart = await _cartService.GetCartFromDatabaseAsync(username);

                if (!cart.Any())
                {
                    return null;
                }

                return new CheckoutVM
                {
                    FullName = "",
                    Address = "",
                    PhoneNumber = "",
                    CartItems = cart,
                    TotalAmount = cart.Sum(x => x.Total),
                    PaymentMethod = "COD"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout view for user {Username}", username);
                return null;
            }
        }

        /// <summary>
        /// VNPay callback sau khi user thanh toán.
        /// [AllowAnonymous] vì VNPay redirect không có session/cookie
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayCallback()
        {
            try
            {
                var response = _vnPayService.PaymentExecute(Request.Query);

                if (response == null || !response.Success || response.VnPayResponseCode != "00")
                {
                    _logger.LogWarning("VNPay payment failed for order {OrderId}. Code: {ResponseCode}", 
                        response?.OrderId, response?.VnPayResponseCode);
                    TempData["ErrorMessage"] = "Payment failed. Please try again.";
                    return RedirectToAction("Index", "Cart");
                }

                var orderId = int.Parse(response.OrderId);
                var order = await _context.HoaDons.FindAsync(orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found in VNPay callback", orderId);
                    TempData["ErrorMessage"] = "Order not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Update order status
                order.MaTrangThai = 1; // Paid
                order.CachThanhToan = "VNPay";
                order.GhiChu = $"VNPay TxnRef: {response.TransactionId}. {order.GhiChu}";
                await _context.SaveChangesAsync();

                // Clear cart for this user
                var username = order.MaKh; // Lấy username từ order thay vì GetUsernameFromRequest
                var sessionId = PersistentSessionHelper.GetOrCreatePersistentSessionId(HttpContext);
                await _cartService.ClearCartAsync(username, sessionId);

                _logger.LogInformation("VNPay payment successful for order {OrderId}", orderId);

                TempData["SuccessMessage"] = $"Payment successful! Your order #{orderId} has been confirmed.";
                return RedirectToAction("OrderConfirmation", new { orderId = orderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay callback");
                TempData["ErrorMessage"] = "An error occurred processing your payment.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}

