using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Data.Models;
using SCMM.Redis.Client;
using SCMM.Steam.Data.Models;
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
        private readonly RedisConnection _cache;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public SystemController(ILogger<SystemController> logger, SteamDbContext db, RedisConnection cache, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _cache = cache;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        /// <summary>
        /// Get app system status
        /// </summary>
        /// <returns>The system status for the current app</returns>
        /// <response code="200">The system status for the current app.</response>
        /// <response code="404">If the request app cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("status")]
        [ProducesResponseType(typeof(SystemAppStatusDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSystemAppStatus()
        {
            var appId = this.App().Guid;
            var app = await _db.SteamApps
                .AsNoTracking()
                .Where(x => x.Id == appId)
                .Select(x => new SystemAppStatusDTO()
                {
                    SteamId = x.SteamId,
                    Name = x.Name,
                    IconUrl = x.IconUrl,
                    ItemDefinitionsDigest = x.ItemDefinitionsDigest,
                    ItemDefinitionsLastModified = x.TimeUpdated,
                    LastCheckedAssetDescriptions = new TimeRangeDTO()
                    {
                        Oldest = x.AssetDescriptions.Min(y => y.TimeRefreshed),
                        Newest = x.AssetDescriptions.Max(y => y.TimeRefreshed),
                    },
                    LastCheckedMarketOrders = new TimeRangeDTO()
                    {
                        Oldest = x.MarketItems.Min(y => y.LastCheckedOrdersOn),
                        Newest = x.MarketItems.Max(y => y.LastCheckedOrdersOn),
                    },
                    LastCheckedMarketSales = new TimeRangeDTO()
                    {
                        Oldest = x.MarketItems.Min(y => y.LastCheckedSalesOn),
                        Newest = x.MarketItems.Max(y => y.LastCheckedSalesOn),
                    }
                })
                .FirstOrDefaultAsync();

            if (app == null)
            {
                return NotFound($"App was not found");
            }

            return Ok(app);
        }

        /// <summary>
        /// Get most recent system update messages
        /// </summary>
        /// <returns>The most recent system update messages</returns>
        /// <response code="200">The most recent system update messages</response>
        /// <response code="404">If the request app cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("latestUpdates")]
        [ProducesResponseType(typeof(IEnumerable<SystemUpdateMessageDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetLatestSystemUpdateMessages()
        {
            var latestSystemUpdates = await _cache.GetAsync<TextMessage[]>(Constants.LatestSystemUpdatesCacheKey);
            if (latestSystemUpdates == null)
            {
                return NotFound("No recent system changes found");
            }

            return Ok(latestSystemUpdates
                .OrderByDescending(x => x.Timestamp)
                .Select(x => new SystemUpdateMessageDTO()
                { 
                    Timestamp = x.Timestamp,
                    Description = x.Content,
                    Media = x.Attachments.ToDictionary(k => k.Url, v => v.ContentType)
                })
                .ToArray()
            );
        }
    }
}
