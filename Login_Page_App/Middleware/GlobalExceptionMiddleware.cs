using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Login_Page_App.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Determine user for logging
                var user = context.User?.Identity?.IsAuthenticated == true
                    ? context.User.Identity?.Name
                    : "anonymous";

                // Log the exception including the User property so it is persisted to the DB column
                Log.ForContext("User", user)
                   .Error(ex, "An unhandled exception occurred while processing the request for {Path}", context.Request.Path);

                if (!context.Response.HasStarted)
                {
                    // Prefer redirecting to shared error page for browser requests
                    context.Response.Clear();
                    context.Response.Redirect("/Home/Error");
                }

                // If response already started, there's not much we can do; rethrow so server handles it
                if (context.Response.HasStarted)
                {
                    throw;
                }
            }
        }
    }
}
