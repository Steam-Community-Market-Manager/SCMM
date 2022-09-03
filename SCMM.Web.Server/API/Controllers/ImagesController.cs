using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/image")]
    [Obsolete("Images are now served from https://data.scmm.app, this API is now obsolete")]
    public class ImageController : ControllerBase
    {
        /// <summary>
        /// Get a cached image (DEPRECATED)
        /// </summary>
        /// <param name="id">Image GUID</param>
        /// <remarks>Range requests are supported.</remarks>
        /// <returns>Image data</returns>
        /// <response code="410"></response>
        [Obsolete("Images are now served from https://data.scmm.app, this API is now obsolete")]
        [AllowAnonymous]
        [HttpGet("{id}")]
        [HttpGet("{id}.{ext}")]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        public IActionResult GetImage(Guid id)
        {
            return new StatusCodeResult(StatusCodes.Status410Gone);
        }
    }
}
