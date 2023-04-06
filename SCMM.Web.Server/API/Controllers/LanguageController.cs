using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Language;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/language")]
    public class LanguageController : ControllerBase
    {
        private readonly ILogger<LanguageController> _logger;
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public LanguageController(ILogger<LanguageController> logger, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        /// <summary>
        /// List all supported languages
        /// </summary>
        /// <returns>List of supported languages</returns>
        /// <response code="200">If <paramref name="detailed"/> is <code>true</code>, the response will be a list of <see cref="LanguageDetailedDTO"/>. If <code>false</code>, the response will be a list of <see cref="LanguageListDTO"/>.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LanguageListDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IEnumerable<LanguageDetailedDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get([FromQuery] bool detailed = false)
        {
            var languages = await _db.SteamLanguages
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Ok(!detailed
                ? languages.Select(x => _mapper.Map<LanguageListDTO>(x)).ToArray()
                : languages.Select(x => _mapper.Map<LanguageDetailedDTO>(x)).ToArray()
            );
        }
    }
}
