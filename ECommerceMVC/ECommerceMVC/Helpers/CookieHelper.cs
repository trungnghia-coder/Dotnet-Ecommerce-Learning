namespace ECommerceMVC.Helpers
{
    public static class CookieHelper
    {
        public static string? GetCookie(HttpContext context, string key)
        {
            return context.Request.Cookies[key];
        }

        public static bool HasToken(HttpContext context)
        {
            return !string.IsNullOrEmpty(GetCookie(context, "fruitables_ac"));
        }
    }
}
