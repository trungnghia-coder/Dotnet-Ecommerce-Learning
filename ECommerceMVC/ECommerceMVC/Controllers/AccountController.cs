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

        public AccountController(Hshop2023Context context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
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
                    var customer = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaKh == model.Username);

                    if (customer == null)
                    {
                        TempData["ErrorMessage"] = "Invalid username or password";
                        ModelState.AddModelError("", "Invalid username or password");
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
                        TempData["ErrorMessage"] = "Invalid username or password";
                        ModelState.AddModelError("", "Invalid username or password");
                        return View(model);
                    }

                    // Store user info in session
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
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
        #endregion
    }
}
