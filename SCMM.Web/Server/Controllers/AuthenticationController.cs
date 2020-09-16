using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace SCMM.Web.Server.Controllers
{
    public class AuthenticationController : Controller
    {
        [HttpGet("~/signin")]
        public IActionResult SignIn([FromQuery] string returnUrl = "/")
        {
            return Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = returnUrl
                }
            );
        }

        [HttpGet("~/signout")]
        public IActionResult SignOut([FromQuery] string returnUrl = "/")
        {
            return SignOut(
                new AuthenticationProperties
                { 
                    RedirectUri = returnUrl
                }
            );
        }
    }
}
