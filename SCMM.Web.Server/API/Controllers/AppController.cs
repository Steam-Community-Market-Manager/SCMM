using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.App;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/app")]
    public class AppController : ControllerBase
    {
        private readonly ILogger<AppController> _logger;
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public AppController(ILogger<AppController> logger, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        /// <summary>
        /// List all supported apps
        /// </summary>
        /// <returns>List of supported apps</returns>
        /// <response code="200">If <paramref name="detailed"/> is <code>true</code>, the response will be a list of <see cref="AppDetailedDTO"/>. If <code>false</code>, the response will be a list of <see cref="AppListDTO"/>.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AppListDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IEnumerable<AppDetailedDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get([FromQuery] bool detailed = false)
        {
            var apps = await _db.SteamApps
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Ok(!detailed
                ? apps.Select(x => _mapper.Map<AppListDTO>(x)).ToList()
                : apps.Select(x => _mapper.Map<AppDetailedDTO>(x)).ToList()
            );
        }
    }
}
