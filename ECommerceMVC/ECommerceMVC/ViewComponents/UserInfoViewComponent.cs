using ECommerceMVC.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceMVC.ViewComponents
{
    public class UserInfoViewComponent : ViewComponent
    {
        private readonly AuthHelper _authHelper;

        public UserInfoViewComponent(AuthHelper authHelper)
        {
            _authHelper = authHelper;
        }

        public IViewComponentResult Invoke()
        {
            var userInfo = _authHelper.GetCurrentUser(HttpContext);
            return View(userInfo);
        }
    }
}
