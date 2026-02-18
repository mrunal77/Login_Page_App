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

        public SerilogMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await _next(context);
                sw.Stop();

                var user = context.User?.Identity?.IsAuthenticated == true
                    ? context.User.Identity?.Name
                    : "anonymous";

                Log.ForContext("RequestPath", context.Request.Path)
                   .ForContext("RequestMethod", context.Request.Method)
                   .ForContext("StatusCode", context.Response?.StatusCode)
                   .ForContext("ElapsedMs", sw.Elapsed.TotalMilliseconds)
                   .ForContext("User", user)
                   .Information("Handled HTTP {Method} {Path} responded {StatusCode} in {Elapsed} ms", context.Request.Method, context.Request.Path, context.Response?.StatusCode, sw.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                var user = context.User?.Identity?.IsAuthenticated == true
                    ? context.User.Identity?.Name
                    : "anonymous";

                Log.ForContext("RequestPath", context.Request.Path)
                   .ForContext("RequestMethod", context.Request.Method)
                   .ForContext("ElapsedMs", sw.Elapsed.TotalMilliseconds)
                   .ForContext("User", user)
                   .Error(ex, "Unhandled exception for HTTP {Method} {Path} after {Elapsed} ms", context.Request.Method, context.Request.Path, sw.Elapsed.TotalMilliseconds);

                throw;
            }
        }
    }
}
