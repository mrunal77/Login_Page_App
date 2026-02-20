using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Login_Page_App.Middleware
{
    public class SerilogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Login_Page_App.Logging.DatabaseLogger? _dbLogger;

        public SerilogMiddleware(RequestDelegate next, Login_Page_App.Logging.DatabaseLogger? dbLogger = null)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _dbLogger = dbLogger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();

            // Determine if this is a login/logout request (handle Identity area paths too)
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
            var isLoginPath = path.Contains("/account/login");
            var isLogoutPath = path.Contains("/account/logout");
            var isLoginLogout = isLoginPath || isLogoutPath;

            // If not a login/logout request, just continue pipeline without extra logging
            if (!isLoginLogout)
            {
                await _next(context);
                return;
            }

            // Capture username before the request runs (useful for logout where sign-out may clear principal)
            var usernameBefore = context.User?.Identity?.IsAuthenticated == true ? context.User.Identity?.Name : null;

            try
            {
                await _next(context);
                sw.Stop();

                // After the request, determine username: prefer authenticated identity, fall back to usernameBefore
                var user = context.User?.Identity?.IsAuthenticated == true
                    ? context.User.Identity?.Name
                    : usernameBefore ?? "anonymous";

                var message = "Handled HTTP {Method} {Path} responded {StatusCode} in {Elapsed} ms";

                Log.ForContext("RequestPath", context.Request.Path)
                   .ForContext("RequestMethod", context.Request.Method)
                   .ForContext("StatusCode", context.Response?.StatusCode)
                   .ForContext("ElapsedMs", sw.Elapsed.TotalMilliseconds)
                   .ForContext("User", user)
                   .ForContext("IsRequest", true)
                   .Information(message, context.Request.Method, context.Request.Path, context.Response?.StatusCode, sw.Elapsed.TotalMilliseconds);

                if (_dbLogger?.Logger != null)
                {
                    _dbLogger.Logger.ForContext("RequestPath", context.Request.Path)
                        .ForContext("RequestMethod", context.Request.Method)
                        .ForContext("StatusCode", context.Response?.StatusCode)
                        .ForContext("ElapsedMs", sw.Elapsed.TotalMilliseconds)
                        .ForContext("User", user)
                        .ForContext("IsRequest", true)
                        .Information(message, context.Request.Method, context.Request.Path, context.Response?.StatusCode, sw.Elapsed.TotalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();

                var user = context.User?.Identity?.IsAuthenticated == true
                    ? context.User.Identity?.Name
                    : usernameBefore ?? "anonymous";

                var messageEx = "Unhandled exception for HTTP {Method} {Path} after {Elapsed} ms";

                Log.ForContext("RequestPath", context.Request.Path)
                   .ForContext("RequestMethod", context.Request.Method)
                   .ForContext("ElapsedMs", sw.Elapsed.TotalMilliseconds)
                   .ForContext("User", user)
                   .ForContext("IsRequest", true)
                   .Error(ex, messageEx, context.Request.Method, context.Request.Path, sw.Elapsed.TotalMilliseconds);

                if (_dbLogger?.Logger != null)
                {
                    _dbLogger.Logger.ForContext("RequestPath", context.Request.Path)
                        .ForContext("RequestMethod", context.Request.Method)
                        .ForContext("ElapsedMs", sw.Elapsed.TotalMilliseconds)
                        .ForContext("User", user)
                        .ForContext("IsRequest", true)
                        .Error(ex, messageEx, context.Request.Method, context.Request.Path, sw.Elapsed.TotalMilliseconds);
                }

                throw;
            }
        }
    }
}
