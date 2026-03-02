using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Login_Page_App.Services;

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

            await _signInManager.RefreshSignInAsync(user);

            var expiry = DateTimeOffset.UtcNow.AddSeconds(30);

            Response.Cookies.Append("AuthExpiry", expiry.ToString("o"), new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = false,
                Expires = expiry,
                Secure = Request.IsHttps,
                Path = "/"
            });

            _sessionTracker.AddOrUpdateSession(user.Id, expiry);

            return Json(new { expiry = expiry.ToString("o") });
        }
    }
}
