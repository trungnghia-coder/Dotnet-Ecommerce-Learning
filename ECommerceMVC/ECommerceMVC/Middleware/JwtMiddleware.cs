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
                        // Set user claims for authorization
                        context.User = principal;

                        _logger.LogInformation($"JWT validated for user: {principal.FindFirst(ClaimTypes.NameIdentifier)?.Value}");
                    }
                    else
                    {
                        // Token invalid, clear cookies
                        _logger.LogWarning("Invalid token detected, clearing cookies");
                        context.Response.Cookies.Delete("fruitables_ac");
                        context.Response.Cookies.Delete("fruitables_rf");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating token");
                    // Clear invalid token
                    context.Response.Cookies.Delete("fruitables_ac");
                    context.Response.Cookies.Delete("fruitables_rf");
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
