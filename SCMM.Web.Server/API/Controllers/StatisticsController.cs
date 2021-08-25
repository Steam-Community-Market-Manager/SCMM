using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Extensions;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.Extensions;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Data.Models.UI.Profile;
using SCMM.Web.Data.Models.UI.Statistic;
using SCMM.Web.Server.Extensions;
using Syncfusion.Blazor.Data;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/stats")]
    public class StatisticsController : ControllerBase
    {
        private readonly ILogger<StatisticsController> _logger;
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public StatisticsController(ILogger<StatisticsController> logger, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        /// <summary>
        /// Get marketplace listing activity from the last 24hrs
        /// </summary>
        /// <param name="filter">Optional filter, matches against the item name</param>
        /// <param name="start">Return activity starting at this specific index (pagination)</param>
        /// <param name="count">Number activity to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of activity matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("market/activity")]
        [ProducesResponseType(typeof(PaginatedResult<ItemActivityStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMarketActivity([FromQuery] string filter = null, [FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamMarketItemActivity
                .AsNoTracking()
                .Include(x => x.Description).ThenInclude(x => x.App)
                .Include(x => x.Currency)
                .Where(x => string.IsNullOrEmpty(filter) || x.Description.Name.Contains(filter) || x.BuyerName.Contains(filter) || x.SellerName.Contains(filter))
                .OrderByDescending(x => x.Timestamp);

            return Ok(
                await query.PaginateAsync(start, count, x => new ItemActivityStatisticDTO()
                {
                    Id = x.Description.ClassId,
                    AppId = ulong.Parse(x.Description.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Timestamp = x.Timestamp,
                    Type = x.Type,
                    Price = this.Currency().CalculateExchange(x.Price, x.Currency),
                    Quantity = x.Quantity,
                    SellerName = x.SellerName,
                    SellerAvatarUrl = x.SellerAvatarUrl,
                    BuyerName = x.BuyerName,
                    BuyerAvatarUrl = x.BuyerAvatarUrl
                })
            );
        }

        /// <summary>
        /// Get items with the highest amount of market listing activity in the last 24hrs
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("market/activityTopItems")]
        [ProducesResponseType(typeof(PaginatedResult<ItemDescriptionDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMarketActivityTopItems([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var topAssetDescriptionIds = await _db.SteamMarketItemActivity
                .AsNoTracking()
                .Where(x => !string.IsNullOrEmpty(x.SellerName) && !string.IsNullOrEmpty(x.BuyerName))
                .GroupBy(x => x.DescriptionId)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .Take(30)
                .ToArrayAsync();

            var query = _db.SteamAssetDescriptions
                .AsNoTracking()
                .Include(x => x.App)
                .Where(x => topAssetDescriptionIds.Contains(x.Id))
                .ToList()
                .OrderBy(x => topAssetDescriptionIds.IndexOf(x.Id))
                .Select(x => new ItemDescriptionDTO()
                {
                    Id = x.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Name,
                    BackgroundColour = x.BackgroundColour,
                    ForegroundColour = x.ForegroundColour,
                    IconUrl = x.IconUrl
                })
                .AsQueryable();

            return Ok(
                query.Paginate(start, count)
            );
        }

        /// <summary>
        /// Get marketplace sales and revenue chart data, grouped by day (UTC)
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <response code="200">List of market sales and revenue per day grouped/keyed by UTC date.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("market/activityTimeline")]
        [ProducesResponseType(typeof(IEnumerable<MarketActivityChartStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMarketActivityTimeline()
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
                    // TODO: Snapshot these for faster querying
                    Sales = x.Sum(y => y.Quantity),
                    Revenue = x.Sum(y => y.Quantity * y.Price)
                });

            var salesPerDay = (await query.ToListAsync()).Select(
                x => new MarketActivityChartStatisticDTO
                {
                    Date = x.Date,
                    Sales = x.Sales,
                    Revenue = this.Currency().ToPrice(this.Currency().CalculateExchange(x.Revenue))
                }
            );

            return Ok(salesPerDay);
        }

        /// <summary>
        /// List items, sorted by most recent first
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/mostRecent")]
        [ProducesResponseType(typeof(PaginatedResult<AssetDescriptionAgeStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsMostRecent([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamAssetDescriptions
                .AsNoTracking()
                .Include(x => x.App)
                .Where(x => x.TimeAccepted != null)
                .OrderByDescending(x => x.TimeAccepted);

            return Ok(
                await query.PaginateAsync(start, count, x => new AssetDescriptionAgeStatisticDTO()
                {
                    Id = x.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Name,
                    BackgroundColour = x.BackgroundColour,
                    ForegroundColour = x.ForegroundColour,
                    IconUrl = x.IconUrl,
                    Age = (DateTimeOffset.Now - x.TimeAccepted).ToMarketAgeString()
                })
            );
        }

        /// <summary>
        /// List items, sorted by highest number of sales in the last 24hrs
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/mostDemanded")]
        [ProducesResponseType(typeof(PaginatedResult<ItemSupplyDemandStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsMostDemanded([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Description)
                .Where(x => x.Last24hrSales > 0)
                .OrderByDescending(x => x.Last24hrSales)
                .Select(x => new ItemSupplyDemandStatisticDTO()
                {
                    Id = x.Description.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Supply = x.Supply,
                    Demand = x.Last24hrSales
                });

            return Ok(
                await query.PaginateAsync(start, count)
            );
        }

        /// <summary>
        /// List items, sorted by highest supply
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/mostSaturated")]
        [ProducesResponseType(typeof(PaginatedResult<ItemSupplyDemandStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsMostSaturated([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Description)
                .Where(x => x.Supply > 0)
                .OrderByDescending(x => x.Supply)
                .Select(x => new ItemSupplyDemandStatisticDTO()
                {
                    Id = x.Description.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Supply = x.Supply,
                    Demand = x.Last24hrSales
                });

            return Ok(
                await query.PaginateAsync(start, count)
            );
        }

        /// <summary>
        /// List items currently at their all-time highest value, sorted by highest value
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/allTimeHigh")]
        [ProducesResponseType(typeof(PaginatedResult<ItemValueStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsAllTimeHigh([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var sevenDays = TimeSpan.FromDays(7);
            var lastFewHours = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromHours(6));
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.LastCheckedSalesOn >= lastFewHours && x.LastCheckedOrdersOn >= lastFewHours)
                .Where(x => (x.Last1hrValue - x.AllTimeHighestValue) >= 0)
                .Where(x => x.Supply > 0)
                .Where(x => x.AllTimeHighestValue > 0)
                .OrderBy(x => Math.Abs(x.Last1hrValue - x.AllTimeHighestValue))
                .ThenByDescending(x => x.Last1hrValue - x.Last24hrValue);

            return Ok(
                await query.PaginateAsync(start, count, x => new ItemValueStatisticDTO()
                {
                    Id = x.Description.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    BuyNowPrice = this.Currency().CalculateExchange(x.BuyNowPrice, x.Currency)
                })
            );
        }

        /// <summary>
        /// List items current at their all-time lowest value, sorted by lowest value
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/allTimeLow")]
        [ProducesResponseType(typeof(PaginatedResult<ItemValueStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsAllTimeLow([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var lastFewHours = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromHours(6));
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.LastCheckedSalesOn >= lastFewHours && x.LastCheckedOrdersOn >= lastFewHours)
                .Where(x => (x.Last1hrValue - x.AllTimeLowestValue) <= 0)
                .Where(x => x.Supply > 0)
                .Where(x => x.AllTimeLowestValue > 0)
                .OrderBy(x => Math.Abs(x.Last1hrValue - x.AllTimeLowestValue))
                .ThenBy(x => x.Last1hrValue - x.Last24hrValue);

            return Ok(
                await query.PaginateAsync(start, count, x => new ItemValueStatisticDTO()
                {
                    Id = x.Description.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    BuyNowPrice = this.Currency().CalculateExchange(x.BuyNowPrice, x.Currency)
                })
            );
        }

        /// <summary>
        /// List items with largest gap between buy now and buy asking price, sorted by highest potential profit
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/profitableFlips")]
        [ProducesResponseType(typeof(PaginatedResult<ItemBuySellOrderStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsProfitableFlips([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var lastFewHours = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromHours(6));
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
                .Where(x => (x.BuyNowPrice - x.BuyAskingPrice - Math.Floor(x.BuyNowPrice * EconomyExtensions.FeeMultiplier)) > 300) // more than $3 profit
                .Where(x => x.LastCheckedSalesOn >= lastFewHours && x.LastCheckedOrdersOn >= lastFewHours)
                .OrderByDescending(x => (x.BuyNowPrice - x.BuyAskingPrice - Math.Floor(x.BuyNowPrice * EconomyExtensions.FeeMultiplier)));

            return Ok(
                await query.PaginateAsync(start, count, x => new ItemBuySellOrderStatisticDTO()
                {
                    Id = x.Description.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    BuyNowPrice = this.Currency().CalculateExchange(x.BuyNowPrice, x.Currency),
                    BuyAskingPrice = this.Currency().CalculateExchange(x.BuyAskingPrice, x.Currency)
                })
            );
        }

        /// <summary>
        /// List items with largest gap between buy now price and average market value
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/manipulated")]
        [ProducesResponseType(typeof(PaginatedResult<ItemManipulationStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsManipulated([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var lastFewHours = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromHours(6));
            var now = DateTimeOffset.UtcNow;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.BuyNowPrice > 0 && x.AllTimeAverageValue > 0)
                .Where(x => x.BuyNowPrice / x.AllTimeAverageValue > 5) // more than 5x average price
                .Where(x => x.LastCheckedSalesOn >= lastFewHours && x.LastCheckedOrdersOn >= lastFewHours)
                .OrderByDescending(x => x.BuyNowPrice / x.AllTimeAverageValue);

            return Ok(
                await query.PaginateAsync(start, count, x => new ItemManipulationStatisticDTO()
                {
                    Id = x.Description.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    AverageMarketValue = this.Currency().CalculateExchange(x.AllTimeAverageValue, x.Currency),
                    BuyNowPrice = this.Currency().CalculateExchange(x.BuyNowPrice, x.Currency)
                })
            );
        }

        /// <summary>
        /// List items, sorted by highest value
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/mostExpensive")]
        [ProducesResponseType(typeof(PaginatedResult<ItemValueStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsMostExpensive([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.BuyNowPrice > 0)
                .OrderByDescending(x => x.BuyNowPrice);

            return Ok(
                await query.PaginateAsync(start, count, x => new ItemValueStatisticDTO()
                {
                    Id = x.Description.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    BuyNowPrice = this.Currency().CalculateExchange(x.BuyNowPrice, x.Currency)
                })
            );
        }

        /// <summary>
        /// List items, sorted by highest number of sales
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/mostSales")]
        [ProducesResponseType(typeof(PaginatedResult<ItemSalesStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsMostSales([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamAssetDescriptions
                .AsNoTracking()
                .Include(x => x.App)
                .Where(x => x.AssetType == SteamAssetDescriptionType.WorkshopItem && x.LifetimeSubscriptions > 0)
                .OrderByDescending(x => x.LifetimeSubscriptions)
                .Select(x => new
                {
                    Item = x,
                    // TODO: Snapshot these for faster querying
                    Subscriptions = (x.LifetimeSubscriptions ?? 0),
                    TotalSalesMin = (x.StoreItem != null ? x.StoreItem.TotalSalesMin ?? 0 : 0),
                    KnownInventoryDuplicates = 0/*x.InventoryItems
                        .GroupBy(y => y.ProfileId)
                        .Where(y => y.Count() > 1)
                        .Select(y => y.Sum(z => z.Quantity))
                        .Sum(x => x)*/
                })
                .Select(x => new ItemSalesStatisticDTO()
                {
                    Id = x.Item.ClassId,
                    AppId = ulong.Parse(x.Item.App.SteamId),
                    Name = x.Item.Name,
                    BackgroundColour = x.Item.BackgroundColour,
                    ForegroundColour = x.Item.ForegroundColour,
                    IconUrl = x.Item.IconUrl,
                    Subscriptions = x.Subscriptions,
                    KnownInventoryDuplicates = x.KnownInventoryDuplicates,
                    EstimatedOtherDuplicates = Math.Max(0, x.TotalSalesMin - x.Subscriptions - x.KnownInventoryDuplicates)
                });

            return Ok(
                await query.PaginateAsync(start, count)
            );
        }

        /// <summary>
        /// List items, grouped by collection name, sorted by highest item count
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/largestCollections")]
        [ProducesResponseType(typeof(PaginatedResult<DashboardAssetCollectionDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsLargestCollections([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamAssetDescriptions
                .AsNoTracking()
                .Include(x => x.App)
                .Where(x => x.ItemCollection != null)
                .Select(x => new
                {
                    CreatorId = x.CreatorId,
                    Name = x.ItemCollection,
                    IconUrl = x.IconLargeUrl,
                    Currency = x.MarketItem != null ? x.MarketItem.Currency : (x.StoreItem != null ? x.StoreItem.Currency : null),
                    BuyNowPrice = x.MarketItem != null ? (long?)x.MarketItem.BuyNowPrice : (x.StoreItem != null ? (long?)x.StoreItem.Price : null)
                })
                .ToList()
                .GroupBy(x => new 
                { 
                    CreatorId = x.CreatorId, 
                    Name = x.Name 
                })
                .OrderByDescending(x => x.Count())
                .AsQueryable();

            return Ok(
                query.Paginate(start, count, x => new DashboardAssetCollectionDTO
                {
                    CreatorId = x.Key.CreatorId,
                    Name = x.Key.Name,
                    IconUrl = x.FirstOrDefault(y => y.BuyNowPrice == x.Max(z => z.BuyNowPrice))?.IconUrl,
                    Items = x.Count(),
                    BuyNowPrice = this.Currency().CalculateExchange(x.Sum(y => y.BuyNowPrice ?? 0), x.FirstOrDefault()?.Currency)
                })
            );
        }

        /// <summary>
        /// List crafting resources/items, sorted by lowest cost
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/cheapestResourcesCosts")]
        [ProducesResponseType(typeof(PaginatedResult<ItemResourceCostStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsCheapestResourceCosts([FromQuery] int start = 0, [FromQuery] int count = 10)
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
                query.Paginate(start, count, x => new ItemResourceCostStatisticDTO
                {
                    Id = x.Resource.ClassId,
                    AppId = ulong.Parse(x.Resource.App.SteamId),
                    Name = x.Resource.Name,
                    BackgroundColour = x.Resource.BackgroundColour,
                    ForegroundColour = x.Resource.ForegroundColour,
                    IconUrl = x.Resource.IconUrl,
                    BuyNowPrice = this.Currency().CalculateExchange(x.Resource.MarketItem.BuyNowPrice, x.Resource.MarketItem.Currency),
                    CheapestItem = new ItemValueStatisticDTO()
                    {
                        Id = x.CheapestItem.Item.ClassId,
                        AppId = ulong.Parse(x.CheapestItem.Item.App.SteamId),
                        Name = x.CheapestItem.Item.Name,
                        BackgroundColour = x.CheapestItem.Item.BackgroundColour,
                        ForegroundColour = x.CheapestItem.Item.ForegroundColour,
                        IconUrl = x.CheapestItem.Item.IconUrl,
                        BuyNowPrice = this.Currency().CalculateExchange(x.CheapestItem.BuyNowPrice, x.CheapestItem.Currency),
                    }
                })
            );
        }

        /// <summary>
        /// List craftable containers/items, sorted by lowest cost
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/cheapestCraftingCosts")]
        [ProducesResponseType(typeof(PaginatedResult<ItemCraftingCostStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsCheapestCraftingCosts([FromQuery] int start = 0, [FromQuery] int count = 10)
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
                query.Paginate(start, count, x => new ItemCraftingCostStatisticDTO
                {
                    Id = x.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Name,
                    BackgroundColour = x.BackgroundColour,
                    ForegroundColour = x.ForegroundColour,
                    IconUrl = x.IconUrl,
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
                        .Select(y => new ItemCraftingComponentCostDTO()
                        {
                            Component = new ItemValueStatisticDTO
                            {
                                Id = y.CheapestItem.ClassId,
                                AppId = ulong.Parse(y.CheapestItem.App.SteamId),
                                Name = y.CheapestItem.Name,
                                BackgroundColour = y.CheapestItem.BackgroundColour,
                                ForegroundColour = y.CheapestItem.ForegroundColour,
                                IconUrl = y.CheapestItem.IconUrl,
                                BuyNowPrice = this.Currency().CalculateExchange(y.CheapestItem.BuyNowPrice ?? 0, y.CheapestItem.BuyNowCurrency),
                            },
                            Quantity = y.Quantity
                        })
                })
            );
        }

        /// <summary>
        /// List profiles with accepted workshop items, sorted by highest number of items
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("profiles/largestCreators")]
        [ProducesResponseType(typeof(PaginatedResult<ProfileAcceptedItemsStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfilesLargestCreators([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamProfiles
                .AsNoTracking()
                .Where(x => x.AssetDescriptions.Count(y => y.TimeAccepted != null) > 0)
                .OrderByDescending(x => x.AssetDescriptions.Count(y => y.TimeAccepted != null))
                .Select(x => new ProfileAcceptedItemsStatisticDTO()
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

        /// <summary>
        /// List profiles who have the donator role, sorted by highest contribution
        /// </summary>
        /// <returns>The list of users who have donated</returns>
        /// <response code="200">The list of users who have donated.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("donators")]
        [ProducesResponseType(typeof(IEnumerable<ProfileDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfilesDonators()
        {
            var donators = await _db.SteamProfiles
                .AsNoTracking()
                .Where(x => x.DonatorLevel > 0)
                .OrderByDescending(x => x.DonatorLevel)
                .Select(x => _mapper.Map<ProfileDTO>(x))
                .ToListAsync();

            return Ok(donators);
        }

        /// <summary>
        /// List profiles who have the contributor role
        /// </summary>
        /// <returns>The list of users who have contributed</returns>
        /// <response code="200">The list of users who have contributed.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("contributors")]
        [ProducesResponseType(typeof(IEnumerable<ProfileDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfilesContributors()
        {
            var donators = await _db.SteamProfiles
                .AsNoTracking()
                .Where(x => x.Roles.Serialised.Contains(Roles.Contributor))
                .OrderByDescending(x => x.SteamId)
                .Select(x => _mapper.Map<ProfileDTO>(x))
                .ToListAsync();

            return Ok(donators);
        }
    }
}
