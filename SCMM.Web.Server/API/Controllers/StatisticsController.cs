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
        /// <param name="filter">Optional filter, matches against the buyer name, seller name, or item name</param>
        /// <param name="item">Optional filter, matches against the item name</param>
        /// <param name="start">Return activity starting at this specific index (pagination)</param>
        /// <param name="count">Number activity to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of activity matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("market/activity")]
        [ProducesResponseType(typeof(PaginatedResult<ItemActivityStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMarketActivity([FromQuery] string filter = null, [FromQuery] string item = null, [FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var appId = this.App().Guid;
            var query = _db.SteamMarketItemActivity
                .AsNoTracking()
                .Include(x => x.Description).ThenInclude(x => x.App)
                .Include(x => x.Currency)
                .Where(x => x.Item.AppId == appId)
                .Where(x => string.IsNullOrEmpty(filter) || x.Description.Name.Contains(filter) || x.BuyerName.Contains(filter) || x.SellerName.Contains(filter))
                .Where(x => string.IsNullOrEmpty(item) || x.Description.Name.Contains(item))
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
            var appId = this.App().Guid;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Description)
                .Where(x => x.AppId == appId)
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
                    Supply = x.SellOrderCount,
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
            var appId = this.App().Guid;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Description)
                .Where(x => x.AppId == appId)
                .Where(x => x.SellOrderCount > 0)
                .OrderByDescending(x => x.SellOrderCount)
                .Select(x => new ItemSupplyDemandStatisticDTO()
                {
                    Id = x.Description.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Supply = x.SellOrderCount,
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
            var appId = this.App().Guid;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.AppId == appId)
                .Where(x => x.LastCheckedSalesOn >= lastFewHours && x.LastCheckedOrdersOn >= lastFewHours)
                .Where(x => x.SellOrderCount > 0)
                .Where(x => x.AllTimeHighestValue > 0)
                .Where(x => (x.SellOrderLowestPrice - x.AllTimeHighestValue) >= 0)
                .OrderBy(x => Math.Abs(x.SellOrderLowestPrice - x.AllTimeHighestValue));

            return Ok(
                await query.PaginateAsync(start, count, x => new ItemValueStatisticDTO()
                {
                    Id = x.Description.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    BuyNowPrice = this.Currency().CalculateExchange(x.SellOrderLowestPrice, x.Currency)
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
            var appId = this.App().Guid;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.AppId == appId)
                .Where(x => x.LastCheckedSalesOn >= lastFewHours && x.LastCheckedOrdersOn >= lastFewHours)
                .Where(x => x.SellOrderCount > 0)
                .Where(x => x.AllTimeLowestValue > 0)
                .Where(x => (x.SellOrderLowestPrice - x.AllTimeLowestValue) <= 0)
                .OrderBy(x => Math.Abs(x.SellOrderLowestPrice - x.AllTimeLowestValue));

            return Ok(
                await query.PaginateAsync(start, count, x => new ItemValueStatisticDTO()
                {
                    Id = x.Description.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    BuyNowPrice = this.Currency().CalculateExchange(x.SellOrderLowestPrice, x.Currency)
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
            var appId = this.App().Guid;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.AppId == appId)
                .Where(x => x.SellOrderLowestPrice > 0)
                .OrderByDescending(x => x.SellOrderLowestPrice);

            return Ok(
                await query.PaginateAsync(start, count, x => new ItemValueStatisticDTO()
                {
                    Id = x.Description.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    BuyNowPrice = this.Currency().CalculateExchange(x.SellOrderLowestPrice, x.Currency)
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
            var appId = this.App().Guid;
            var query = _db.SteamAssetDescriptions
                .AsNoTracking()
                .Include(x => x.App)
                .Where(x => x.AppId == appId)
                .Where(x => x.AssetType == SteamAssetDescriptionType.WorkshopItem && x.LifetimeSubscriptions > 0)
                .OrderByDescending(x => x.LifetimeSubscriptions)
                .Select(x => new
                {
                    Item = x,
                    // TODO: Snapshot these for faster querying
                    Subscriptions = (x.LifetimeSubscriptions ?? 0),
                    TotalSalesMin = (x.StoreItem != null ? (x.StoreItem.TotalSalesMin ?? 0) : 0),
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
            var appId = this.App().Guid;
            var query = _db.SteamAssetDescriptions
                .AsNoTracking()
                .Include(x => x.App)
                .Where(x => x.AppId == appId)
                .Where(x => x.ItemCollection != null)
                .Select(x => new
                {
                    CreatorId = (!x.IsSpecialDrop && !x.IsTwitchDrop) ? x.CreatorId : null,
                    Name = x.ItemCollection,
                    IconUrl = x.IconLargeUrl,
                    // NOTE: This isn't 100% accurate if the store item price is used. Update this to use StoreItem.Prices with the local currency
                    BuyNowPrice = x.MarketItem != null ? (long?)x.MarketItem.SellOrderLowestPrice : (x.StoreItem != null ? (long?)x.StoreItem.Price : null),
                    Currency = x.MarketItem != null ? x.MarketItem.Currency : (x.StoreItem != null ? x.StoreItem.Currency : null)
                })
                .ToList()
                .GroupBy(x => new
                {
                    CreatorId = x.CreatorId,
                    Name = x.Name
                })
                .OrderByDescending(x => x.Count())
                .ThenByDescending(x => x.Sum(y => y.BuyNowPrice ?? 0))
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
            var appId = this.App().Guid;
            var query = _db.SteamProfiles
                .AsNoTracking()
                .Where(x => x.AssetDescriptions.Count(y => y.AppId == appId && y.TimeAccepted != null) > 0)
                .OrderByDescending(x => x.AssetDescriptions.Count(y => y.AppId == appId && y.TimeAccepted != null))
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
