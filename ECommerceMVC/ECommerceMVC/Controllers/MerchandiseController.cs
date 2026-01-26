using Microsoft.AspNetCore.Mvc;

namespace ECommerceMVC.Controllers
{
    public class MerchandiseController : Controller
    {
        public IActionResult Index(int? category)
        {
            return View();
        }
    }
}
