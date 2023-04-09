using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Currency;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/currency")]
    public class CurrencyController : ControllerBase
    {
        private readonly ILogger<CurrencyController> _logger;
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public CurrencyController(ILogger<CurrencyController> logger, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        /// <summary>
        /// List all supported currencies
        /// </summary>
        /// <returns>List of supported currencies</returns>
        /// <response code="200">If <paramref name="detailed"/> is <code>true</code>, the response will be a list of <see cref="CurrencyDetailedDTO"/>. If <code>false</code>, the response will be a list of <see cref="CurrencyListDTO"/>.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CurrencyListDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IEnumerable<CurrencyDetailedDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OutputCache(PolicyName = Policies.Cache1Hour)]
        public async Task<IActionResult> Get([FromQuery] bool detailed = false)
        {
            var currencies = await _db.SteamCurrencies
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Ok(!detailed
                ? currencies.Select(x => _mapper.Map<CurrencyListDTO>(x)).ToArray()
                : currencies.Select(x => _mapper.Map<CurrencyDetailedDTO>(x)).ToArray()
            );
        }
    }
}
