using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceMVC.Controllers
{
    public class MerchandiseController : Controller
    {
        private readonly Hshop2023Context db;

        public MerchandiseController(Hshop2023Context context)
        {
            db = context;
        }
        public IActionResult Index(int? category, string? query)
        {
            var merchandises = db.HangHoas.AsQueryable();
            
            // Lọc theo category
            if (category.HasValue)
            {
                merchandises = merchandises.Where(p => p.MaLoai == category.Value);
            }

            // Tìm kiếm theo tên sản phẩm hoặc tên category
            if (!string.IsNullOrEmpty(query))
            {
                merchandises = merchandises.Where(p => 
                    p.TenHh.Contains(query) || 
                    p.MaLoaiNavigation.TenLoai.Contains(query));
                
                ViewBag.SearchQuery = query;
            }

            var result = merchandises.Select(p => new MerchandiseVM
            {
                MerchandiseId = p.MaHh,
                Merchandisename = p.TenHh,
                Price = p.DonGia ?? 0,
                Image = p.Hinh ?? "",
                Description = p.MoTaDonVi ?? "",
                CategoryName = p.MaLoaiNavigation.TenLoai
            });
            
            return View(result);
        }
    }
}
