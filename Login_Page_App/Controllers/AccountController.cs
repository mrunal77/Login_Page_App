using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Login_Page_App.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost]
        [Route("Account/ExtendSession")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ExtendSession()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Refresh the sign-in cookie to extend session
            await _signInManager.RefreshSignInAsync(user);

            // New expiry (match the cookie ExpireTimeSpan configured in Program.cs — 30 seconds)
            var expiry = DateTimeOffset.UtcNow.AddSeconds(30);

            // Set a client-visible cookie so JS can detect expiry
            Response.Cookies.Append("AuthExpiry", expiry.ToString("o"), new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = false,
                Expires = expiry,
                Secure = Request.IsHttps,
                Path = "/"
            });

            return Json(new { expiry = expiry.ToString("o") });
        }
    }
}

