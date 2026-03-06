using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Login_Page_App.Services;

namespace Login_Page_App.Middleware
{
    public class AuthExpiryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TimeSpan _expiry = TimeSpan.FromSeconds(30);
        private readonly ISessionTracker? _sessionTracker;

        public AuthExpiryMiddleware(RequestDelegate next, ISessionTracker? sessionTracker = null)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _sessionTracker = sessionTracker;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                var expiry = DateTimeOffset.UtcNow.Add(_expiry);
                context.Response.Cookies.Append("AuthExpiry", expiry.ToString("o"), new CookieOptions
                {
                    HttpOnly = false,
                    Expires = expiry,
                    Secure = context.Request.IsHttps,
                    Path = "/",
                    SameSite = SameSiteMode.Lax
                });

                if (!string.IsNullOrEmpty(userId) && _sessionTracker != null)
                {
                    _sessionTracker.AddOrUpdateSession(userId, expiry);
                }
            }

            await _next(context);
        }
    }
}
