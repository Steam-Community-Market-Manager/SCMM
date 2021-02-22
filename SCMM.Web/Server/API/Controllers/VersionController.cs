using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/version")]
    public class VersionController : ControllerBase
    {
        private readonly ILogger<StoreController> _logger;

        public VersionController(ILogger<StoreController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get the server application version
        /// </summary>
        /// <returns>Server application version</returns>
        /// <response code="200">The server application version.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Get()
        {
            var server = Assembly.GetExecutingAssembly().GetName();
            return Ok(
                $"{server.Version.Major}.{server.Version.Minor}"
            );
        }
    }
}
