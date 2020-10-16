using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VersionController : ControllerBase
    {
        private readonly ILogger<StoreController> _logger;

        public VersionController(ILogger<StoreController> logger)
        {
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet]
        public string Get()
        {
            var server = Assembly.GetExecutingAssembly().GetName();
            return $"{server.Version.Major}.{server.Version.Minor}";
        }
    }
}
