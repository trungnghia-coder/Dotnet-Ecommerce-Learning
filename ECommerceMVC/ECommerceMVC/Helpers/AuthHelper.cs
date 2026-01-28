using System.Security.Claims;

namespace ECommerceMVC.Helpers
{
    public class AuthHelper
    {
        private readonly JwtHelper _jwtHelper;

        public AuthHelper(JwtHelper jwtHelper)
        {
            _jwtHelper = jwtHelper;
        }

        public UserInfo? GetCurrentUser(HttpContext httpContext)
        {
            var token = httpContext.Request.Cookies["fruitables_ac"];
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            var principal = _jwtHelper.ValidateToken(token);
            if (principal == null)
            {
                return null;
            }

            var username = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var fullName = principal.FindFirst(ClaimTypes.Name)?.Value;
            var roleStr = principal.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            return new UserInfo
            {
                Username = username,
                FullName = fullName ?? "",
                Role = int.TryParse(roleStr, out int role) ? role : 0
            };
        }
        public bool IsAuthenticated(HttpContext httpContext)
        {
            return GetCurrentUser(httpContext) != null;
        }
    }

    public class UserInfo
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int Role { get; set; }
    }
}
