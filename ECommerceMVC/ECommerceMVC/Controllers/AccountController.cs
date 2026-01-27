using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly Hshop2023Context _context;
        private readonly ILogger<AccountController> _logger;
        private readonly JwtHelper _jwtHelper;
        private readonly IConfiguration _configuration;

        public AccountController(Hshop2023Context context, ILogger<AccountController> logger, JwtHelper jwtHelper, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _jwtHelper = jwtHelper;
            _configuration = configuration;
        }

        #region Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Generate unique username
                    var username = PasswordHelper.GenerateUsername(model.FullName, model.Email);
                    
                    // Ensure username is unique
                    var existingUser = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaKh == username);
                    while (existingUser != null)
                    {
                        username = PasswordHelper.GenerateUsername(model.FullName, model.Email);
                        existingUser = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaKh == username);
                    }

                    // Check if email already exists
                    var existingEmail = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Email == model.Email);
                    if (existingEmail != null)
                    {
                        ModelState.AddModelError("Email", "Email already exists");
                        TempData["ErrorMessage"] = "Email already exists. Please use another email.";
                        return View(model);
                    }

                    // Create new customer
                    var randomKey = PasswordHelper.GenerateRandomKey();
                    var hashedPassword = PasswordHelper.HashPassword(model.Password, randomKey);

                    var newCustomer = new KhachHang
                    {
                        MaKh = username,
                        MatKhau = hashedPassword,
                        HoTen = model.FullName,
                        Email = model.Email,
                        DienThoai = model.PhoneNumber,
                        DiaChi = model.Address,
                        GioiTinh = model.Gender,
                        NgaySinh = model.DateOfBirth,
                        HieuLuc = true,
                        VaiTro = 0, // 0 = Customer, 1 = Admin
                        RandomKey = randomKey,
                        Hinh = "default-avatar.jpg"
                    };

                    _context.KhachHangs.Add(newCustomer);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"New customer registered successfully: {username} - {model.Email}");
                    
                    TempData["SuccessMessage"] = $"Registration successful! Your username is: <strong>{username}</strong>. Please login.";
                    return RedirectToAction("Login");
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error during registration");
                    TempData["ErrorMessage"] = $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}";
                    ModelState.AddModelError("", "Database error occurred. Please try again.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during registration");
                    TempData["ErrorMessage"] = $"Error: {ex.Message}";
                    ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Please correct the errors in the form.";
            }

            return View(model);
        }
        #endregion

        #region Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Find user by email or phone
                    var customer = await _context.KhachHangs
                        .FirstOrDefaultAsync(k => k.Email == model.EmailOrPhone || k.DienThoai == model.EmailOrPhone);

                    if (customer == null)
                    {
                        TempData["ErrorMessage"] = "Invalid email/phone or password";
                        ModelState.AddModelError("", "Invalid email/phone or password");
                        return View(model);
                    }

                    if (!customer.HieuLuc)
                    {
                        TempData["ErrorMessage"] = "Account is disabled. Please contact administrator.";
                        ModelState.AddModelError("", "Account is disabled. Please contact administrator.");
                        return View(model);
                    }

                    // Verify password
                    var isValidPassword = PasswordHelper.VerifyPassword(model.Password, customer.RandomKey ?? "", customer.MatKhau ?? "");

                    if (!isValidPassword)
                    {
                        TempData["ErrorMessage"] = "Invalid email/phone or password";
                        ModelState.AddModelError("", "Invalid email/phone or password");
                        return View(model);
                    }

                    // Generate access token
                    var accessToken = _jwtHelper.GenerateAccessToken(customer.MaKh, customer.HoTen, customer.VaiTro);

                    // Set cookie options
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    };

                    if (model.RememberMe)
                    {
                        // Remember me: Generate refresh token and set both cookies with expiration
                        var refreshToken = _jwtHelper.GenerateRefreshToken();
                        var refreshTokenExpiration = DateTime.UtcNow.AddDays(int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]!));

                        // Save refresh token to session
                        HttpContext.Session.SetString("RefreshToken", refreshToken);
                        HttpContext.Session.SetString("RefreshTokenExpiration", refreshTokenExpiration.ToString());

                        // Set cookies with expiration
                        cookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]!));
                        Response.Cookies.Append("fruitables_ac", accessToken, cookieOptions);
                        Response.Cookies.Append("fruitables_rf", refreshToken, cookieOptions);

                        _logger.LogInformation($"User logged in with Remember Me: {customer.MaKh}");
                    }
                    else
                    {
                        // Session-only login: Only set access token as session cookie (no expiration)
                        Response.Cookies.Append("fruitables_ac", accessToken, cookieOptions);
                        
                        _logger.LogInformation($"User logged in (session-only): {customer.MaKh}");
                    }

                    // Store user info in session as fallback
                    HttpContext.Session.SetString("Username", customer.MaKh);
                    HttpContext.Session.SetString("FullName", customer.HoTen);
                    HttpContext.Session.SetInt32("Role", customer.VaiTro);

                    _logger.LogInformation($"User logged in: {customer.MaKh}");
                    
                    TempData["SuccessMessage"] = $"Welcome back, {customer.HoTen}!";

                    // Redirect
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during login");
                    TempData["ErrorMessage"] = $"Login error: {ex.Message}";
                    ModelState.AddModelError("", "An error occurred during login. Please try again.");
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Please fill in all required fields.";
            }

            return View(model);
        }
        #endregion

        #region Logout
        public IActionResult Logout()
        {
            // Clear session
            HttpContext.Session.Clear();
            
            // Clear cookies
            Response.Cookies.Delete("fruitables_ac");
            Response.Cookies.Delete("fruitables_rf");
            
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }
        #endregion

        #region RefreshToken
        [HttpPost]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var refreshToken = Request.Cookies["fruitables_rf"];
                
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return Json(new { success = false, message = "No refresh token found" });
                }

                // Validate refresh token from session
                var storedRefreshToken = HttpContext.Session.GetString("RefreshToken");
                var refreshTokenExpiration = HttpContext.Session.GetString("RefreshTokenExpiration");

                if (storedRefreshToken != refreshToken)
                {
                    return Json(new { success = false, message = "Invalid refresh token" });
                }

                if (DateTime.Parse(refreshTokenExpiration!) < DateTime.UtcNow)
                {
                    return Json(new { success = false, message = "Refresh token expired" });
                }

                // Get user from session
                var username = HttpContext.Session.GetString("Username");
                var customer = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaKh == username);

                if (customer == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Generate new access token
                var newAccessToken = _jwtHelper.GenerateAccessToken(customer.MaKh, customer.HoTen, customer.VaiTro);
                var accessTokenExpiration = _jwtHelper.GetTokenExpiration(newAccessToken);

                // Set new access token cookie
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]!))
                };

                Response.Cookies.Append("fruitables_ac", newAccessToken, cookieOptions);

                return Json(new 
                { 
                    success = true, 
                    accessToken = newAccessToken,
                    expiresAt = accessTokenExpiration.ToString("o")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return Json(new { success = false, message = "Error refreshing token" });
            }
        }
        #endregion
    }
}
