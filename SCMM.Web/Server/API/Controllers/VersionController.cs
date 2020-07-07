using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace SCMM.Web.Server.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("[controller]")]
    public class VersionController : ControllerBase
    {
        private readonly ILogger<StoreItemsController> _logger;

        public VersionController(ILogger<StoreItemsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public string Get()
        {
            var server = Assembly.GetExecutingAssembly().GetName();
            return $"{server.Version.Major}.{server.Version.Minor}";
        }
    }
}
