using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceMVC.ViewComponents
{
    public class CategoryViewComponent : ViewComponent
    {
        private readonly Hshop2023Context db;

        public CategoryViewComponent(Hshop2023Context context) => db = context;

        public IViewComponentResult Invoke()
        {
            var data = db.Loais.Select(cate => new CategoryMenuVM
            {
                Id = cate.MaLoai,
                Name = cate.TenLoai,
                Quantity = cate.HangHoas.Count
            });

            return View(data);
        }
    }
}
