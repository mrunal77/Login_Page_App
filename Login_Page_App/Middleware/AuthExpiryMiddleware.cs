using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Login_Page_App.Middleware
{
    public class AuthExpiryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TimeSpan _expiry = TimeSpan.FromSeconds(30);

        public AuthExpiryMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only set client-visible AuthExpiry for authenticated users when missing
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var existing = context.Request.Cookies["AuthExpiry"];
                if (string.IsNullOrEmpty(existing))
                {
                    var expiry = DateTimeOffset.UtcNow.Add(_expiry);
                    context.Response.Cookies.Append("AuthExpiry", expiry.ToString("o"), new CookieOptions
                    {
                        HttpOnly = false,
                        Expires = expiry,
                        Secure = context.Request.IsHttps,
                        Path = "/"
                    });
                }
            }

            await _next(context);
        }
    }
}
