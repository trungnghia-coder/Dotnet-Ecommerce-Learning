using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceMVC.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly Hshop2023Context _context;

        public CategoryMenuViewComponent(Hshop2023Context context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var data = _context.Loais
                .Select(lo => new CategoryMenuVM
                {
                    Id = lo.MaLoai,
                    Name = lo.TenLoai,
                    Quantity = lo.HangHoas.Count
                }).OrderBy(p => p.Name);

            return View(data);
        }
    }
}
