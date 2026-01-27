using ECommerceMVC.Helpers;
using System.Security.Claims;

namespace ECommerceMVC.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtMiddleware> _logger;

        public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, JwtHelper jwtHelper)
        {
            // Get token from cookie
            var token = context.Request.Cookies["fruitables_ac"];

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    // Validate token
                    var principal = jwtHelper.ValidateToken(token);

                    if (principal != null)
                    {
                        // Extract user info from token
                        var username = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var fullName = principal.FindFirst(ClaimTypes.Name)?.Value;
                        var role = principal.FindFirst(ClaimTypes.Role)?.Value;

                        // Restore session if empty
                        if (string.IsNullOrEmpty(context.Session.GetString("Username")) && !string.IsNullOrEmpty(username))
                        {
                            context.Session.SetString("Username", username);
                            context.Session.SetString("FullName", fullName ?? "");
                            context.Session.SetInt32("Role", int.Parse(role ?? "0"));

                            _logger.LogInformation($"Session restored from token for user: {username}");
                        }

                        // Set user claims
                        context.User = principal;
                    }
                    else
                    {
                        // Token invalid, clear cookies
                        _logger.LogWarning("Invalid token detected, clearing cookies");
                        context.Response.Cookies.Delete("fruitables_ac");
                        context.Response.Cookies.Delete("fruitables_rf");
                        context.Session.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating token");
                    // Clear invalid token
                    context.Response.Cookies.Delete("fruitables_ac");
                    context.Response.Cookies.Delete("fruitables_rf");
                    context.Session.Clear();
                }
            }

            await _next(context);
        }
    }

    // Extension method for middleware
    public static class JwtMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtMiddleware>();
        }
    }
}
