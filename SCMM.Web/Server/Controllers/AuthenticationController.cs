using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace SCMM.Web.Server.Controllers
{
    public class AuthenticationController : Controller
    {
        /// <summary>
        /// Sign-in via Steam OpenId
        /// </summary>
        /// <remarks>Redirect client browser to this endpoint, not a Web API</remarks>
        /// <param name="returnUrl">Where to redirect the client after signing in</param>
        /// <returns>A redirection to Steam's OpenId</returns>
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

        /// <summary>
        /// Sign-out
        /// </summary>
        /// <remarks>Redirect client browser to this endpoint, not a Web API</remarks>
        /// <param name="returnUrl">Where to redirect the client after signing out</param>
        /// <returns>A redirection to <paramref name="returnUrl"/></returns>
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
