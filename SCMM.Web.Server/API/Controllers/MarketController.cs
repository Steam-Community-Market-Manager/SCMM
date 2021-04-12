using AutoMapper;
using AutoMapper.QueryableExtensions;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Data.Shared;
using SCMM.Data.Shared.Extensions;
using SCMM.Data.Shared.Store.Extensions;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Models.Steam;
using SCMM.Web.Data.Models.Domain.DTOs.MarketItems;
using SCMM.Web.Data.Models.Extensions;
using SCMM.Web.Data.Models.UI.MarketStatistics;
using SCMM.Web.Server.Extensions;
using Skclusive.Core.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/market")]
    public class MarketController : ControllerBase
    {
        private readonly ILogger<MarketController> _logger;
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public MarketController(ILogger<MarketController> logger, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        /// <summary>
        /// List market listings
        /// </summary>
        /// <remarks>
        /// Item <code>ProfileFlags</code> will only be populated if the request is authenticated.
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="filter">Optional search filter. Matches against item name or description</param>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <param name="sortBy">Sort item property name from <see cref="MarketItemListDTO"/></param>
        /// <param name="sortDirection">Sort item direction</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<MarketItemListDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get([FromQuery] string filter = null, [FromQuery] int start = 0, [FromQuery] int count = 10, [FromQuery] string sortBy = null, [FromQuery] Sort sortDirection = Sort.Ascending)
        {
            if (start < 0)
            {
                return BadRequest("Start index must be a positive number");
            }
            if (count <= 0)
            {
                return BadRequest("Item count must be greater than zero");
            }

            filter = Uri.UnescapeDataString(filter?.Trim() ?? String.Empty);
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Include(x => x.Description.WorkshopFile)
                .Include(x => x.Description.StoreItem)
                .Where(x => String.IsNullOrEmpty(filter) || x.Description.Name.Contains(filter) || x.Description.Tags.Serialised.Contains(filter))
                .OrderByDescending(x => x.Last24hrSales);

            if (User.Identity.IsAuthenticated)
            {
                // Paginate and return with our user profile flags
                var profileId = User.Id();
                var profileMarketItems = await _db.SteamProfileMarketItems
                    .AsNoTracking()
                    .Where(x => x.ProfileId == profileId)
                    .Select(x => new
                    {
                        SteamDescriptionId = x.Description.SteamId,
                        Flags = x.Flags
                    })
                    .ToListAsync();

                var results = query.Paginate(start, count, x =>
                {
                    var marketItem = _mapper.Map<SteamMarketItem, MarketItemListDTO>(x, this);
                    var profileMarketItem = profileMarketItems.FirstOrDefault(y => y.SteamDescriptionId == marketItem.SteamDescriptionId);
                    if (profileMarketItem != null)
                    {
                        marketItem.ProfileFlags = profileMarketItem.Flags;
                    }

                    return marketItem;
                });

                return Ok(results);
            }
            else
            {
                // Paginate and return
                var results = await query.PaginateAsync(start, count,
                    x => _mapper.Map<SteamMarketItem, MarketItemListDTO>(x, this)
                );

                return Ok(results);
            }
        }

        /// <summary>
        /// Get market listing details
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="idOrName">Market item identifier or name</param>
        /// <response code="200">Market item details.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="400">If the request item cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("item/{idOrName}")]
        [ProducesResponseType(typeof(MarketItemListDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get([FromRoute] string idOrName)
        {
            if (String.IsNullOrEmpty(idOrName))
            {
                return BadRequest("Item id/name is invalid");
            }

            var id = Guid.Empty;
            Guid.TryParse(idOrName, out id);

            var item = await _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Include(x => x.Description.WorkshopFile)
                .FirstOrDefaultAsync(x => (id != Guid.Empty && id == x.Id) || (id == Guid.Empty && idOrName == x.Description.Name));

            if (item == null)
            {
                return NotFound($"Unable to find any market item matching \"{idOrName}\"");
            }

            return Ok(
                _mapper.Map<SteamMarketItem, MarketItemListDTO>(item, this)
            );
        }

        /// <summary>
        /// Get number of marketplace sales yesterday (UTC)
        /// </summary>
        /// <response code="200">Number of marketplace sales yesterday (UTC).</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("stat/salesCountYesterday")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSalesToday()
        {
            var from = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(2));
            var to = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var query = _db.SteamMarketItemSale
                .AsNoTracking()
                .Where(x => x.Timestamp.Date >= from && x.Timestamp.Date <= to.Date)
                .GroupBy(x => x.Timestamp.Date)
                .OrderByDescending(x => x.Key.Date)
                .Select(x => x.Sum(y => y.Quantity));

            return Ok(
                await query.SingleOrDefaultAsync()
            );
        }

        /// <summary>
        /// Get marketplace sales grouped per day (UTC)
        /// </summary>
        /// <param name="maxDays">Number of historic days from today. If <code>null</code>,all sales history is returned</param>
        /// <response code="200">Dictionary of total market sales per day grouped/keyed by UTC date.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("stat/salesPerDay")]
        [ProducesResponseType(typeof(IDictionary<string, DashboardSalesDataDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSalesPerDay([FromQuery] int? maxDays = 30)
        {
            var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var query = _db.SteamMarketItemSale
                .AsNoTracking()
                .Where(x => x.Timestamp.Date <= yesterday.Date)
                .GroupBy(x => x.Timestamp.Date)
                .OrderByDescending(x => x.Key.Date)
                .Select(x => new
                {
                    Date = x.Key,
                    Sales = x.Sum(y => y.Quantity),
                    Revenue = x.Sum(y => y.Quantity * y.Price)
                });

            if (maxDays > 0)
            {
                query = query.Take(maxDays.Value);
            }

            var salesPerDay = await query.ToListAsync();
            salesPerDay.Reverse(); // newest at bottom
            return Ok(
                salesPerDay.ToDictionary(
                    x => x.Date.ToString("dd MMM yyyy"),
                    x => new DashboardSalesDataDTO
                    {
                        Sales = x.Sales,
                        Revenue = x.Revenue
                    }
                )
            );
        }

        /// <summary>
        /// List items sorted by highest number of sales in the last 24hrs
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="200">Dictionary of total market sales per day grouped/keyed by UTC date.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("stat/hotRightNow")]
        [ProducesResponseType(typeof(PaginatedResult<DashboardAssetSalesDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardHotRightNow([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Description)
                .Where(x => x.Last24hrSales > 0)
                .OrderByDescending(x => x.Last24hrSales)
                .Select(x => new DashboardAssetSalesDTO()
                {
                    SteamId = x.Description.SteamId,
                    SteamAppId = x.App.SteamId,
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Last24hrSales = x.Last24hrSales
                });

            return Ok(
                await query.PaginateAsync(start, count)
            );
        }

        /// <summary>
        /// List items sorted by lowest market age
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="200">Dictionary of total market sales per day grouped/keyed by UTC date.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("stat/mostRecent")]
        [ProducesResponseType(typeof(PaginatedResult<DashboardAssetAgeDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardMostRecent([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Description)
                .Where(x => x.FirstSeenOn != null)
                .OrderByDescending(x => x.FirstSeenOn);

            return Ok(
                await query.PaginateAsync(start, count, x => new DashboardAssetAgeDTO()
                {
                    SteamId = x.Description.SteamId,
                    SteamAppId = x.App.SteamId,
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    MarketAge = x.MarketAge.ToMarketAgeString()
                })
            );
        }

        /// <summary>
        /// List items currently at their all-time highest value, sorted by highest value
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="200">Dictionary of total market sales per day grouped/keyed by UTC date.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("stat/allTimeHigh")]
        [ProducesResponseType(typeof(PaginatedResult<DashboardAssetMarketValueDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardAllTimeHigh([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => (x.Last1hrValue - x.AllTimeHighestValue) >= 0)
                .Where(x => x.SalesHistory.Max(y => y.Timestamp) >= yesterday)
                .OrderBy(x => Math.Abs(x.Last1hrValue - x.AllTimeHighestValue))
                .ThenByDescending(x => x.Last1hrValue - x.Last24hrValue);

            return Ok(
                await query.PaginateAsync(start, count, x => new DashboardAssetMarketValueDTO()
                {
                    SteamId = x.Description.SteamId,
                    SteamAppId = x.App.SteamId,
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Currency = this.Currency(),
                    Last1hrValue = this.Currency().CalculateExchange(x.Last1hrValue, x.Currency)
                })
            );
        }

        /// <summary>
        /// List items current at their all-time lowest value, sorted by lowest value
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="200">Dictionary of total market sales per day grouped/keyed by UTC date.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("stat/allTimeLow")]
        [ProducesResponseType(typeof(PaginatedResult<DashboardAssetMarketValueDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardAllTimeLow([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => (x.Last1hrValue - x.AllTimeLowestValue) <= 0)
                .Where(x => x.SalesHistory.Max(y => y.Timestamp) >= yesterday)
                .OrderBy(x => Math.Abs(x.Last1hrValue - x.AllTimeLowestValue))
                .ThenBy(x => x.Last1hrValue - x.Last24hrValue);

            return Ok(
                await query.PaginateAsync(start, count, x => new DashboardAssetMarketValueDTO()
                {
                    SteamId = x.Description.SteamId,
                    SteamAppId = x.App.SteamId,
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Currency = this.Currency(),
                    Last1hrValue = this.Currency().CalculateExchange(x.Last1hrValue, x.Currency)
                })
            );
        }

        /// <summary>
        /// List items with largest gap between buy now and buy asking price, sorted by highest potential profit
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="200">Dictionary of total market sales per day grouped/keyed by UTC date.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("stat/profitableFlips")]
        [ProducesResponseType(typeof(PaginatedResult<DashboardAssetBuyOrderValueDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardProfitableFlips([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var now = DateTimeOffset.UtcNow;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.Last24hrValue > x.Last168hrValue)
                .Where(x => x.BuyAskingPrice < x.Last24hrValue)
                .Where(x => x.BuyAskingPrice > x.AllTimeLowestValue)
                .Where(x => x.BuyNowPrice > x.Last24hrValue)
                .Where(x => x.BuyNowPrice < x.AllTimeHighestValue)
                .Where(x => (x.BuyNowPrice - x.BuyAskingPrice - Math.Floor(x.BuyNowPrice * EconomyExtensions.FeeMultiplier)) > 0)
                .OrderByDescending(x => (x.BuyNowPrice - x.BuyAskingPrice - Math.Floor(x.BuyNowPrice * EconomyExtensions.FeeMultiplier)));

            return Ok(
                await query.PaginateAsync(start, count, x => new DashboardAssetBuyOrderValueDTO()
                {
                    SteamId = x.Description.SteamId,
                    SteamAppId = x.App.SteamId,
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Currency = this.Currency(),
                    BuyNowPrice = this.Currency().CalculateExchange(x.BuyNowPrice, x.Currency),
                    BuyAskingPrice = this.Currency().CalculateExchange(x.BuyAskingPrice, x.Currency)
                })
            );
        }

        /// <summary>
        /// List items sorted by highest value
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="200">Dictionary of total market sales per day grouped/keyed by UTC date.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("stat/mostProfitable")]
        [ProducesResponseType(typeof(PaginatedResult<DashboardAssetMarketValueDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardMostProfitable([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.Last1hrValue > 0)
                .OrderByDescending(x => x.Last1hrValue);

            return Ok(
                await query.PaginateAsync(start, count, x => new DashboardAssetMarketValueDTO()
                {
                    SteamId = x.Description.SteamId,
                    SteamAppId = x.App.SteamId,
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Currency = this.Currency(),
                    Last1hrValue = this.Currency().CalculateExchange(x.Last1hrValue, x.Currency)
                })
            );
        }

        /// <summary>
        /// List items sorted by highest subscriber count
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="200">Dictionary of total market sales per day grouped/keyed by UTC date.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("stat/mostPopular")]
        [ProducesResponseType(typeof(PaginatedResult<DashboardAssetSubscriptionsDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardMostPopular([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamAssetDescriptions
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.WorkshopFile)
                .Where(x => x.WorkshopFile != null && x.WorkshopFile.Subscriptions > 0)
                .OrderByDescending(x => x.WorkshopFile.Subscriptions)
                .Select(x => new DashboardAssetSubscriptionsDTO()
                {
                    SteamId = x.SteamId,
                    SteamAppId = x.App.SteamId,
                    Name = x.Name,
                    BackgroundColour = x.BackgroundColour,
                    ForegroundColour = x.ForegroundColour,
                    IconUrl = x.IconUrl,
                    Subscriptions = x.WorkshopFile.Subscriptions
                });

            return Ok(
                await query.PaginateAsync(start, count)
            );
        }

        /// <summary>
        /// List items sorted by highest supply
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="200">Dictionary of total market sales per day grouped/keyed by UTC date.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("stat/mostSaturated")]
        [ProducesResponseType(typeof(PaginatedResult<DashboardAssetSupplyDemandDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardMostSaturated([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Description)
                .Where(x => x.Supply > 0)
                .OrderByDescending(x => x.Supply)
                .Select(x => new DashboardAssetSupplyDemandDTO()
                {
                    SteamId = x.Description.SteamId,
                    SteamAppId = x.App.SteamId,
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Supply = x.Supply,
                    Demand = (int)x.Last24hrSales
                });

            return Ok(
                await query.PaginateAsync(start, count)
            );
        }

        /// <summary>
        /// List profiles with accepted workshop items, sorted by highest number of items
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="200">Dictionary of total market sales per day grouped/keyed by UTC date.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("stat/acceptedCreators")]
        [ProducesResponseType(typeof(PaginatedResult<DashboardProfileWorkshopValueDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardAcceptedCreators([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamProfiles
                .AsNoTracking()
                .Where(x => x.WorkshopFiles.Count > 0)
                .OrderByDescending(x => x.WorkshopFiles.Count)
                .Select(x => new DashboardProfileWorkshopValueDTO()
                {
                    SteamId = x.SteamId,
                    Name = x.Name,
                    AvatarUrl = x.AvatarUrl,
                    Items = x.WorkshopFiles.Count,
                });

            return Ok(
                await query.PaginateAsync(start, count)
            );
        }
    }
}
