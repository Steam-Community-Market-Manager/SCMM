using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.System;
using SCMM.Web.Server.Extensions;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/system")]
    public class SystemController : ControllerBase
    {
        private readonly ILogger<SystemController> _logger;
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public SystemController(ILogger<SystemController> logger, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        /// <summary>
        /// Get app system status
        /// </summary>
        /// <returns>The system status for the current app</returns>
        /// <response code="200">The system status for the current app.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("status")]
        [ProducesResponseType(typeof(AppStatusDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStatus()
        {
            var appId = this.App();
            var app = await _db.SteamApps
                .AsNoTracking()
                .Where(x => x.SteamId == appId)
                .Select(x => new AppStatusDTO()
                {
                    SteamId = x.SteamId,
                    Name = x.Name,
                    IconUrl = x.IconUrl,
                    ItemDefinitionsDigest = x.ItemDefinitionsDigest,
                    ItemDefinitionsLastModified = x.TimeUpdated,
                    LastCheckedAssetDescriptions = new Tuple<DateTimeOffset?, DateTimeOffset?>(
                        x.AssetDescriptions.Min(y => y.TimeRefreshed),
                        x.AssetDescriptions.Max(y => y.TimeRefreshed)
                    ),
                    LastCheckedMarketOrders = new Tuple<DateTimeOffset?, DateTimeOffset?>(
                        x.MarketItems.Min(y => y.LastCheckedOrdersOn),
                        x.MarketItems.Max(y => y.LastCheckedOrdersOn)
                    ),
                    LastCheckedMarketSales = new Tuple<DateTimeOffset?, DateTimeOffset?>(
                        x.MarketItems.Min(y => y.LastCheckedSalesOn),
                        x.MarketItems.Max(y => y.LastCheckedSalesOn)
                    )
                })
                .FirstOrDefaultAsync();

            return Ok(app);
        }
    }
}
