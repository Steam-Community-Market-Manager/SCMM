using AutoMapper;
using AutoMapper.QueryableExtensions;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Extensions;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models;
using SCMM.Web.Data.Models.Domain.MarketItems;
using SCMM.Web.Data.Models.Extensions;
using SCMM.Web.Data.Models.UI.MarketStatistics;
using SCMM.Web.Server.Extensions;
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
        public async Task<IActionResult> Get([FromQuery] string filter = null, [FromQuery] int start = 0, [FromQuery] int count = 10, [FromQuery] string sortBy = null, [FromQuery] SortDirection sortDirection = SortDirection.Ascending)
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
                        SteamAssetId = x.Description.ClassId,
                        Flags = x.Flags
                    })
                    .ToListAsync();

                var results = query.Paginate(start, count, x =>
                {
                    var marketItem = _mapper.Map<SteamMarketItem, MarketItemListDTO>(x, this);
                    var profileMarketItem = profileMarketItems.FirstOrDefault(y => y.SteamAssetId.ToString() == marketItem.SteamDescriptionId);
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
        /// Get total marketplace sales and revenue, grouped by day (UTC)
        /// </summary>
        /// <response code="200">Dictionary of total market sales per day grouped/keyed by UTC date.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("stat/salesPerDay")]
        [ProducesResponseType(typeof(IEnumerable<DashboardSalesDataDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSalesPerDay()
        {
            var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var query = _db.SteamMarketItemSale
                .AsNoTracking()
                .Where(x => x.Timestamp.Date <= yesterday.Date)
                .GroupBy(x => x.Timestamp.Date)
                .OrderBy(x => x.Key.Date)
                .Select(x => new
                {
                    Date = x.Key,
                    Sales = x.Sum(y => y.Quantity),
                    Revenue = x.Sum(y => y.Quantity * y.Price)
                });

            var salesPerDay = (await query.ToListAsync()).Select(
                x => new DashboardSalesDataDTO
                {
                    Date = x.Date,
                    Sales = x.Sales,
                    Revenue = this.Currency().ToPrice(this.Currency().CalculateExchange(x.Revenue))
                }
            );

            return Ok(salesPerDay);
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
                    SteamId = x.Description.ClassId.ToString(),
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
            var query = _db.SteamAssetDescriptions
                .AsNoTracking()
                .Include(x => x.App)
                .Where(x => x.TimeAccepted != null)
                .OrderByDescending(x => x.TimeAccepted);

            return Ok(
                await query.PaginateAsync(start, count, x => new DashboardAssetAgeDTO()
                {
                    SteamId = x.ClassId.ToString(),
                    SteamAppId = x.App.SteamId,
                    Name = x.Name,
                    BackgroundColour = x.BackgroundColour,
                    ForegroundColour = x.ForegroundColour,
                    IconUrl = x.IconUrl,
                    MarketAge = (DateTimeOffset.Now - x.TimeAccepted).ToMarketAgeString()
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
            var lastFewHours = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromHours(12));
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.LastCheckedSalesOn >= lastFewHours && x.LastCheckedOrdersOn >= lastFewHours)
                .Where(x => (x.BuyNowPrice - x.AllTimeHighestValue) >= 0)
                .OrderBy(x => Math.Abs(x.BuyNowPrice - x.AllTimeHighestValue))
                .ThenByDescending(x => x.BuyNowPrice - x.Last24hrValue);

            return Ok(
                await query.PaginateAsync(start, count, x => new DashboardAssetMarketValueDTO()
                {
                    SteamId = x.Description.ClassId.ToString(),
                    SteamAppId = x.App.SteamId,
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Currency = this.Currency(),
                    BuyNowPrice = this.Currency().CalculateExchange(x.BuyNowPrice, x.Currency)
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
            var lastFewHours = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromHours(12));
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.LastCheckedSalesOn >= lastFewHours && x.LastCheckedOrdersOn >= lastFewHours)
                .Where(x => (x.BuyNowPrice - x.AllTimeLowestValue) <= 0)
                .OrderBy(x => Math.Abs(x.BuyNowPrice - x.AllTimeLowestValue))
                .ThenBy(x => x.BuyNowPrice - x.Last24hrValue);

            return Ok(
                await query.PaginateAsync(start, count, x => new DashboardAssetMarketValueDTO()
                {
                    SteamId = x.Description.ClassId.ToString(),
                    SteamAppId = x.App.SteamId,
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Currency = this.Currency(),
                    BuyNowPrice = this.Currency().CalculateExchange(x.BuyNowPrice, x.Currency)
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
            var lastFewHours = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromHours(12));
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
                .Where(x => x.LastCheckedSalesOn >= lastFewHours && x.LastCheckedOrdersOn >= lastFewHours)
                .OrderByDescending(x => (x.BuyNowPrice - x.BuyAskingPrice - Math.Floor(x.BuyNowPrice * EconomyExtensions.FeeMultiplier)));

            return Ok(
                await query.PaginateAsync(start, count, x => new DashboardAssetBuyOrderValueDTO()
                {
                    SteamId = x.Description.ClassId.ToString(),
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
                .Where(x => x.BuyNowPrice > 0)
                .OrderByDescending(x => x.BuyNowPrice);

            return Ok(
                await query.PaginateAsync(start, count, x => new DashboardAssetMarketValueDTO()
                {
                    SteamId = x.Description.ClassId.ToString(),
                    SteamAppId = x.App.SteamId,
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Currency = this.Currency(),
                    BuyNowPrice = this.Currency().CalculateExchange(x.BuyNowPrice, x.Currency)
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
                .Where(x => x.AssetType == SteamAssetDescriptionType.WorkshopItem && x.TotalSubscriptions > 0)
                .OrderByDescending(x => x.TotalSubscriptions)
                .Select(x => new DashboardAssetSubscriptionsDTO()
                {
                    SteamId = x.ClassId.ToString(),
                    SteamAppId = x.App.SteamId,
                    Name = x.Name,
                    BackgroundColour = x.BackgroundColour,
                    ForegroundColour = x.ForegroundColour,
                    IconUrl = x.IconUrl,
                    Subscriptions = x.TotalSubscriptions ?? 0
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
                    SteamId = x.Description.ClassId.ToString(),
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
                .Where(x => x.AssetDescriptions.Count(y => y.TimeAccepted != null) > 0)
                .OrderByDescending(x => x.AssetDescriptions.Count(y => y.TimeAccepted != null))
                .Select(x => new DashboardProfileWorkshopValueDTO()
                {
                    SteamId = x.SteamId,
                    Name = x.Name,
                    AvatarUrl = x.AvatarUrl,
                    Items = x.AssetDescriptions.Count(y => y.TimeAccepted != null),
                });

            return Ok(
                await query.PaginateAsync(start, count)
            );
        }

        [AllowAnonymous]
        [HttpGet("stat/collections")]
        [ProducesResponseType(typeof(PaginatedResult<DashboardAssetCollectionDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardCollections([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamAssetDescriptions
                .AsNoTracking()
                .Include(x => x.App)
                .Where(x => x.ItemCollection != null)
                .Select(x => new
                {
                    Name = x.ItemCollection,
                    IconUrl = x.IconLargeUrl,
                    Currency = x.MarketItem != null ? x.MarketItem.Currency : null,
                    BuyNowPrice = x.MarketItem != null ? (long?) x.MarketItem.BuyNowPrice : null
                })
                .ToList()
                .GroupBy(x => x.Name)
                .OrderByDescending(x => x.Count())
                .AsQueryable();

            return Ok(
                query.Paginate(start, count, x => new DashboardAssetCollectionDTO
                {
                    Name = x.Key,
                    IconUrl = x.FirstOrDefault(y => y.BuyNowPrice == x.Max(z => z.BuyNowPrice))?.IconUrl,
                    Items = x.Count(),
                    BuyNowPrice = this.Currency().CalculateExchange(x.Sum(y => y.BuyNowPrice ?? 0), x.FirstOrDefault()?.Currency)
                })
            );
        }

        [AllowAnonymous]
        [HttpGet("stat/craftingResources")]
        [ProducesResponseType(typeof(PaginatedResult<DashboardCraftingResourceCostDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardCraftingResource([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamAssetDescriptions
                .Include(x => x.App)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .Where(x => x.IsCraftingComponent)
                .Where(x => x.MarketItem != null)
                .Select(x => new
                {
                    Resource = x,
                    CheapestItem = x.App.AssetDescriptions
                        .Where(y => y.IsBreakable && y.BreaksIntoComponents.Serialised.Contains(x.Name))
                        .Where(y => y.MarketItem != null && y.MarketItem.BuyNowPrice > 0)
                        .OrderBy(y => y.MarketItem.BuyNowPrice)
                        .Select(y => new
                        {
                            Item = y,
                            Currency = y.MarketItem.Currency,
                            BuyNowPrice = y.MarketItem.BuyNowPrice,
                        })
                        .FirstOrDefault()
                })
                .OrderBy(x => x.Resource.MarketItem.BuyNowPrice);

            return Ok(
                query.Paginate(start, count, x => new DashboardCraftingResourceCostDTO
                {
                    SteamId = x.Resource.ClassId.ToString(),
                    SteamAppId = x.Resource.App.SteamId,
                    Name = x.Resource.Name,
                    BackgroundColour = x.Resource.BackgroundColour,
                    ForegroundColour = x.Resource.ForegroundColour,
                    IconUrl = x.Resource.IconUrl,
                    Currency = this.Currency(),
                    BuyNowPrice = this.Currency().CalculateExchange(x.Resource.MarketItem.BuyNowPrice, x.Resource.MarketItem.Currency),
                    CheapestItem = new DashboardAssetMarketValueDTO()
                    {
                        SteamId = x.CheapestItem.Item.ClassId.ToString(),
                        SteamAppId = x.CheapestItem.Item.App.SteamId,
                        Name = x.CheapestItem.Item.Name,
                        BackgroundColour = x.CheapestItem.Item.BackgroundColour,
                        ForegroundColour = x.CheapestItem.Item.ForegroundColour,
                        IconUrl = x.CheapestItem.Item.IconUrl,
                        Currency = this.Currency(),
                        BuyNowPrice = this.Currency().CalculateExchange(x.CheapestItem.BuyNowPrice, x.CheapestItem.Currency),
                    }
                })
            );
        }

        [AllowAnonymous]
        [HttpGet("stat/craftableContainers")]
        [ProducesResponseType(typeof(PaginatedResult<DashboardCraftableContainerCostDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardCraftableContainers([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var resources = await _db.SteamAssetDescriptions
                .Include(x => x.App)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .Where(x => x.IsCraftingComponent)
                .Where(x => x.MarketItem != null)
                .Select(x => new
                {
                    Resource = x,
                    CheapestItem = x.App.AssetDescriptions
                        .Where(y => y.IsBreakable && y.BreaksIntoComponents.Serialised.Contains(x.Name))
                        .Where(y => y.MarketItem != null && y.MarketItem.BuyNowPrice > 0)
                        .OrderBy(y => y.MarketItem.BuyNowPrice)
                        .Select(y => new
                        {
                            Item = y,
                            Currency = y.MarketItem.Currency,
                            MarketItem = y.MarketItem,
                        })
                        .FirstOrDefault()
                })
                .OrderBy(x => x.Resource.MarketItem.BuyNowPrice)
                .ToListAsync();

            var query = _db.SteamAssetDescriptions
                .Include(x => x.App)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .Where(x => x.IsCraftable)
                .Where(x => x.MarketItem != null)
                .OrderBy(x => x.MarketItem.BuyNowPrice);

            return Ok(
                query.Paginate(start, count, x => new DashboardCraftableContainerCostDTO
                {
                    SteamId = x.ClassId.ToString(),
                    SteamAppId = x.App.SteamId,
                    Name = x.Name,
                    BackgroundColour = x.BackgroundColour,
                    ForegroundColour = x.ForegroundColour,
                    IconUrl = x.IconUrl,
                    Currency = this.Currency(),
                    BuyNowPrice = this.Currency().CalculateExchange(x.MarketItem.BuyNowPrice, x.MarketItem.Currency),
                    CraftingComponents = x.CraftingComponents
                        .Select(y => new
                        {
                            Component = resources.FirstOrDefault(r => y.Key == r.Resource.Name),
                            Quantity = y.Value
                        })
                        .Select(y => new
                        {
                            Component = y.Component,
                            CheapestItem = y.Component.CheapestItem.MarketItem.BuyNowPrice > y.Component.Resource.BuyNowPrice ? y.Component.Resource : y.Component.CheapestItem.Item,
                            Quantity = y.Quantity
                        })
                        .Select(y => new DashboardCraftableContainerComponentCostDTO()
                        {
                            Component = new DashboardAssetMarketValueDTO
                            {
                                SteamId = y.CheapestItem.ClassId.ToString(),
                                SteamAppId = y.CheapestItem.App.SteamId,
                                Name = y.CheapestItem.Name,
                                BackgroundColour = y.CheapestItem.BackgroundColour,
                                ForegroundColour = y.CheapestItem.ForegroundColour,
                                IconUrl = y.CheapestItem.IconUrl,
                                Currency = this.Currency(),
                                BuyNowPrice = this.Currency().CalculateExchange(y.CheapestItem.BuyNowPrice ?? 0, y.CheapestItem.BuyNowCurrency),
                            },
                            Quantity = y.Quantity
                        })
                })
            );
        }
    }
}
