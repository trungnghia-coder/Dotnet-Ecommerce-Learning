namespace ECommerceMVC.Helpers
{
    public static class PersistentSessionHelper
    {
        private const string PERSISTENT_SESSION_KEY = "fruitables_psid";
        private const int EXPIRATION_DAYS = 30;

        /// <summary>
        /// Get or create persistent session ID (stored in cookie for 30 days)
        /// </summary>
        public static string GetOrCreatePersistentSessionId(HttpContext context)
        {
            // Try to get from cookie
            var persistentSessionId = context.Request.Cookies[PERSISTENT_SESSION_KEY];

            if (string.IsNullOrEmpty(persistentSessionId))
            {
                // Create new persistent session ID
                persistentSessionId = Guid.NewGuid().ToString("N");

                // Save to cookie (30 days)
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(EXPIRATION_DAYS)
                };

                context.Response.Cookies.Append(PERSISTENT_SESSION_KEY, persistentSessionId, cookieOptions);
            }

            return persistentSessionId;
        }

        /// <summary>
        /// Clear persistent session ID
        /// </summary>
        public static void ClearPersistentSessionId(HttpContext context)
        {
            context.Response.Cookies.Delete(PERSISTENT_SESSION_KEY);
        }
    }
}
