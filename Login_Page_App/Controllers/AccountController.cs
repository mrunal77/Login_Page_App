using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Login_Page_App.Services;
using Microsoft.AspNetCore.Authentication;

namespace Login_Page_App.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ISessionTracker _sessionTracker;

        public AccountController(
            UserManager<IdentityUser> userManager, 
            SignInManager<IdentityUser> signInManager,
            ISessionTracker sessionTracker)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _sessionTracker = sessionTracker;
        }

        [HttpPost]
        [Route("Account/ExtendSession")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ExtendSession()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(30)
            };
            
            await _signInManager.SignInAsync(user, authProperties);

            var expiry = DateTimeOffset.UtcNow.AddSeconds(30);

            var cookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = false,
                Expires = expiry,
                Secure = Request.IsHttps,
                Path = "/",
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax
            };

            Response.Cookies.Append("AuthExpiry", expiry.ToString("o"), cookieOptions);

            _sessionTracker.AddOrUpdateSession(user.Id, expiry);

            return Json(new { expiry = expiry.ToString("o") });
        }

        [Route("Account/Logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            Response.Cookies.Delete("AuthCookie");
            Response.Cookies.Delete("AuthExpiry");
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }
    }
}
