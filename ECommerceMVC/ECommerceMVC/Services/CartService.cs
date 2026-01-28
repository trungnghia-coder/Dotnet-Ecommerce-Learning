using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMVC.Services
{
    public class CartService
    {
        private readonly Hshop2023Context _context;
        private readonly ILogger<CartService> _logger;
        private const int CART_EXPIRATION_DAYS = 30; // Th?i gian l?u cart cho anonymous users

        public CartService(Hshop2023Context context, ILogger<CartService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Get Cart
        // Get cart from database for logged-in user
        public async Task<List<CartItemVM>> GetCartFromDatabaseAsync(string maKh)
        {
            var cartItems = await _context.GioHangs
                .Where(g => g.MaKh == maKh)
                .Include(g => g.HangHoa)
                .Select(g => new CartItemVM
                {
                    MerchandiseId = g.MaHh,
                    Merchandisename = g.HangHoa!.TenHh,
                    Image = g.HangHoa.Hinh ?? "",
                    Price = g.HangHoa.DonGia ?? 0,
                    Quantity = g.SoLuong
                })
                .ToListAsync();

            return cartItems;
        }

        // Get cart from database for anonymous user by session ID
        public async Task<List<CartItemVM>> GetCartFromDatabaseBySessionAsync(string sessionId)
        {
            var cartItems = await _context.GioHangs
                .Where(g => g.SessionId == sessionId && g.MaKh == null)
                .Include(g => g.HangHoa)
                .Select(g => new CartItemVM
                {
                    MerchandiseId = g.MaHh,
                    Merchandisename = g.HangHoa!.TenHh,
                    Image = g.HangHoa.Hinh ?? "",
                    Price = g.HangHoa.DonGia ?? 0,
                    Quantity = g.SoLuong
                })
                .ToListAsync();

            return cartItems;
        }
        #endregion

        #region Add/Update Cart
        // Add or update cart item in database
        public async Task<bool> AddOrUpdateCartItemAsync(string? maKh, string sessionId, int maHh, int quantity)
        {
            try
            {
                var existingItem = await _context.GioHangs
                    .FirstOrDefaultAsync(g => 
                        (maKh != null ? g.MaKh == maKh : g.SessionId == sessionId && g.MaKh == null) 
                        && g.MaHh == maHh);

                if (existingItem != null)
                {
                    existingItem.SoLuong += quantity;
                    existingItem.NgayCapNhat = DateTime.Now;
                }
                else
                {
                    var newItem = new GioHang
                    {
                        MaKh = maKh,
                        SessionId = sessionId,
                        MaHh = maHh,
                        SoLuong = quantity,
                        NgayTao = DateTime.Now,
                        NgayCapNhat = DateTime.Now
                    };
                    _context.GioHangs.Add(newItem);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding/updating cart item");
                return false;
            }
        }
        #endregion

        #region Update Quantity
        public async Task<bool> UpdateQuantityAsync(string? maKh, string sessionId, int maHh, int quantity)
        {
            try
            {
                var item = await _context.GioHangs
                    .FirstOrDefaultAsync(g => 
                        (maKh != null ? g.MaKh == maKh : g.SessionId == sessionId && g.MaKh == null) 
                        && g.MaHh == maHh);

                if (item != null)
                {
                    if (quantity <= 0)
                    {
                        _context.GioHangs.Remove(item);
                    }
                    else
                    {
                        item.SoLuong = quantity;
                        item.NgayCapNhat = DateTime.Now;
                    }

                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quantity");
                return false;
            }
        }
        #endregion

        #region Remove Item
        public async Task<bool> RemoveItemAsync(string? maKh, string sessionId, int maHh)
        {
            try
            {
                var item = await _context.GioHangs
                    .FirstOrDefaultAsync(g => 
                        (maKh != null ? g.MaKh == maKh : g.SessionId == sessionId && g.MaKh == null) 
                        && g.MaHh == maHh);

                if (item != null)
                {
                    _context.GioHangs.Remove(item);
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item");
                return false;
            }
        }
        #endregion

        #region Clear Cart
        public async Task<bool> ClearCartAsync(string? maKh, string sessionId)
        {
            try
            {
                var items = await _context.GioHangs
                    .Where(g => maKh != null ? g.MaKh == maKh : g.SessionId == sessionId && g.MaKh == null)
                    .ToListAsync();

                _context.GioHangs.RemoveRange(items);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                return false;
            }
        }
        #endregion

        #region Merge Cart
        // Merge anonymous cart to logged-in user cart
        public async Task<bool> MergeCartAsync(string sessionId, string maKh)
        {
            try
            {
                // Get anonymous cart items
                var anonymousCartItems = await _context.GioHangs
                    .Where(g => g.SessionId == sessionId && g.MaKh == null)
                    .ToListAsync();

                if (!anonymousCartItems.Any())
                {
                    _logger.LogInformation($"No anonymous cart items to merge for session {sessionId}");
                    return true;
                }

                // Get user's existing cart items
                var userCartItems = await _context.GioHangs
                    .Where(g => g.MaKh == maKh)
                    .ToListAsync();

                foreach (var anonymousItem in anonymousCartItems)
                {
                    var existingUserItem = userCartItems.FirstOrDefault(u => u.MaHh == anonymousItem.MaHh);

                    if (existingUserItem != null)
                    {
                        // Merge quantities (c?ng d?n)
                        existingUserItem.SoLuong += anonymousItem.SoLuong;
                        existingUserItem.NgayCapNhat = DateTime.Now;
                    }
                    else
                    {
                        // Create new item for user (không transfer, t?o m?i)
                        var newUserItem = new GioHang
                        {
                            MaKh = maKh,
                            SessionId = sessionId,
                            MaHh = anonymousItem.MaHh,
                            SoLuong = anonymousItem.SoLuong,
                            NgayTao = DateTime.Now,
                            NgayCapNhat = DateTime.Now
                        };
                        _context.GioHangs.Add(newUserItem);
                    }
                }

                // XÓA T?T C? anonymous cart items sau khi merge
                _context.GioHangs.RemoveRange(anonymousCartItems);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Merged and removed {anonymousCartItems.Count} anonymous items from session {sessionId} to user {maKh}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error merging cart for session {sessionId} to user {maKh}");
                return false;
            }
        }
        #endregion

        #region Clean Old Carts
        // Clean up old anonymous carts (older than CART_EXPIRATION_DAYS)
        public async Task CleanOldAnonymousCartsAsync()
        {
            try
            {
                var expirationDate = DateTime.Now.AddDays(-CART_EXPIRATION_DAYS);
                
                var oldCarts = await _context.GioHangs
                    .Where(g => g.MaKh == null && g.NgayCapNhat < expirationDate)
                    .ToListAsync();

                if (oldCarts.Any())
                {
                    _context.GioHangs.RemoveRange(oldCarts);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Cleaned up {oldCarts.Count} old anonymous cart items");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning old anonymous carts");
            }
        }
        #endregion
    }
}
