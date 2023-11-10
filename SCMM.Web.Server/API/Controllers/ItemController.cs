using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Extensions;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Models.WebApi.Models;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models;
using SCMM.Web.Data.Models.Extensions;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Server.Extensions;
using System.Text.Json;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/item")]
    public class ItemController : ControllerBase
    {
        private readonly ILogger<ItemController> _logger;
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public ItemController(ILogger<ItemController> logger, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        /// <summary>
        /// Search for items
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="id">Optional array of ids to be included in the list. Helpful when you want search for items and compare against a known (but unrelated) item</param>
        /// <param name="filter">Optional search filter. Matches against item GUID, ID64, name, description, author, type, collection, and tags</param>
        /// <param name="exactMatch">If true, only items that exactly match <code>filter</code> are returned. If false, partial matches are returned</param>
        /// <param name="type">If specified, only items matching the supplied item type are returned</param>
        /// <param name="collection">If specified, only items matching the supplied item collection are returned</param>
        /// <param name="creatorId">If specified, only items published by the supplied creator id are returned</param>
        /// <param name="breaksIntoComponent">If specified, only items that break in to this component are returned</param>
        /// <param name="tag">If specified, only items that contain ALL of the supplied tags are returned</param>
        /// <param name="glow">If <code>true</code>, only items tagged with 'glow' are returned</param>
        /// <param name="glowsight">If <code>true</code>, only items tagged with 'glowsight' are returned</param>
        /// <param name="cutout">If <code>true</code>, only items tagged with 'cutout' are returned</param>
        /// <param name="commodity">If <code>true</code>, only commodity items are returned</param>
        /// <param name="marketable">If <code>true</code>, only marketable items are returned</param>
        /// <param name="tradable">If <code>true</code>, only tradable items are returned</param>
        /// <param name="returning">If <code>true</code>, only items that have been released over multiple stores are returned</param>
        /// <param name="banned">If <code>true</code>, only banned items are returned</param>
        /// <param name="publisherDrop">If <code>true</code>, only game publisher drops are returned</param>
        /// <param name="twitchDrop">If <code>true</code>, only twitch drops are returned</param>
        /// <param name="lootCrateDrop">If <code>true</code>, only items which can drop from loot crates are returned</param>
        /// <param name="craftable">If <code>true</code>, only craftable items are returned</param>
        /// <param name="manipulated">If <code>true</code>, only items currently flagged as undergoing price manipulation are returned</param>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 5000</param>
        /// <param name="sortBy">Sort item property name from <see cref="ItemDetailedDTO"/></param>
        /// <param name="sortDirection">Sort item direction</param>
        /// <param name="detailed">If <code>true</code>, the response will be a paginated list of <see cref="ItemDetailedDTO"/>. If <code>false</code>, the response will be a paginated list of <see cref="ItemDescriptionWithPriceDTO"/></param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="404">If no items match the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<ItemDescriptionWithPriceDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(PaginatedResult<ItemDetailedDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItems([FromQuery] ulong?[] id = null, [FromQuery] string filter = null, [FromQuery] bool exactMatch = false, [FromQuery] string type = null, [FromQuery] string collection = null, [FromQuery] ulong? creatorId = null, [FromQuery] string breaksIntoComponent = null, [FromQuery] string[] tag = null,
                                                  [FromQuery] bool? glow = null, [FromQuery] bool? glowsight = null, [FromQuery] bool? cutout = null, [FromQuery] bool? commodity = null, [FromQuery] bool? marketable = null, [FromQuery] bool? tradable = null,
                                                  [FromQuery] bool? returning = null, [FromQuery] bool? banned = null, [FromQuery] bool? publisherDrop = null, [FromQuery] bool? twitchDrop = null, [FromQuery] bool? lootCrateDrop = null, [FromQuery] bool? craftable = null, [FromQuery] bool? manipulated = null,
                                                  [FromQuery] int start = 0, [FromQuery] int count = 10, [FromQuery] string sortBy = null, [FromQuery] SortDirection sortDirection = SortDirection.Ascending, [FromQuery] bool detailed = false)
        {
            id = (id ?? new ulong?[0]);
            if (start < 0)
            {
                return BadRequest("Start index must be a positive number");
            }
            if (count <= 0)
            {
                count = int.MaxValue;
            }

            // Parse all app asset filters
            var appId = this.App().Guid;
            var appFilters = await _db.SteamApps.AsNoTracking()
                .Where(x => x.Id == appId)
                .SelectMany(x => x.AssetFilters)
                .Where(x => x.IsEnabled)
                .ToListAsync();
            var assetFilters = new Dictionary<string, string>();
            foreach (var appFilter in appFilters)
            {
                var queryStringKey = $"filter.{appFilter.SteamId}";
                if (Request.Query.ContainsKey(queryStringKey))
                {
                    assetFilters[appFilter.SteamId] = Request.Query[queryStringKey];
                }
            }

            // Filter app
            var query = _db.SteamAssetDescriptions.AsNoTracking()
                .Where(x => x.AppId == appId);

            // Filter search
            filter = Uri.UnescapeDataString(filter?.Trim() ?? string.Empty);
            if (exactMatch)
            {
                query = query.Where(x =>
                    id.Contains(x.ClassId) ||
                    x.Id.ToString() == filter ||
                    x.ClassId.ToString() == filter ||
                    x.Name == filter ||
                    x.Description == filter ||
                    (x.CreatorProfile != null && x.CreatorProfile.Name == filter) ||
                    x.ItemType == filter ||
                    x.ItemCollection == filter ||
                    x.Tags.Serialised.Contains($"{PersistableStringDictionary.DefaultKeyValueSeperator}{filter}")
                );
            }
            else
            {
                var filterWords = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var filterWord in filterWords)
                {
                    query = query.Where(x =>
                        id.Contains(x.ClassId) ||
                        x.Id.ToString() == filterWord ||
                        x.ClassId.ToString() == filterWord ||
                        x.Name.Contains(filterWord) ||
                        x.Description.Contains(filterWord) ||
                        (x.CreatorProfile != null && x.CreatorProfile.Name.Contains(filterWord)) ||
                        x.ItemType.Contains(filterWord) ||
                        x.ItemCollection.Contains(filterWord) ||
                        x.Tags.Serialised.Contains(filterWord)
                    );
                }
            }

            // Filter toggles
            if (!string.IsNullOrEmpty(type))
            {
                var itemTypes = type.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (itemTypes.Any())
                {
                    query = query.Where(x => id.Contains(x.ClassId) || itemTypes.Contains(x.ItemType));
                }
            }
            if (!string.IsNullOrEmpty(collection))
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.ItemCollection == collection);
            }
            if (creatorId != null)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.CreatorId == creatorId || (x.CreatorProfile != null && x.CreatorProfile.SteamId == creatorId.ToString()) || (x.App != null && x.App.SteamId == creatorId.ToString()));
            }
            if (!String.IsNullOrEmpty(breaksIntoComponent))
            {
                query = query.Where(x => x.BreaksIntoComponents.Serialised.Contains(Uri.EscapeDataString(breaksIntoComponent)));
            }
            if (tag != null && tag.Length > 0)
            {
                foreach (var tagValue in tag)
                {
                    query = query.Where(x => x.Tags.Serialised.Contains(Uri.EscapeDataString(tagValue)));
                }
            }
            if (assetFilters != null && assetFilters.Count > 0)
            {
                foreach (var assetFilter in assetFilters)
                {
                    query = query.Where(x => x.Tags.Serialised.Contains($"{Uri.EscapeDataString(assetFilter.Key)}{PersistableStringDictionary.DefaultKeyValueSeperator}{Uri.EscapeDataString(assetFilter.Value)}"));
                }
            }
            if (glow != null)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.HasGlow == glow.Value);
            }
            if (glowsight != null)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.HasGlowSights == glowsight.Value);
            }
            if (cutout != null)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.HasCutout == cutout.Value);
            }
            if (commodity != null)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.IsCommodity == commodity.Value);
            }
            if (marketable != null)
            {
                query = query.Where(x => id.Contains(x.ClassId) || (x.IsMarketable == marketable.Value || x.MarketableRestrictionDays > 0));
            }
            if (tradable != null)
            {
                query = query.Where(x => id.Contains(x.ClassId) || (x.IsTradable == tradable.Value || x.TradableRestrictionDays > 0 || (x.IsTradable && x.TradableRestrictionDays == null)));
            }
            if (returning != null)
            {
                query = query.Where(x => id.Contains(x.ClassId) || (x.StoreItem != null && x.StoreItem.HasReturnedToStore == returning.Value));
            }
            if (banned != null)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.IsBanned == banned.Value);
            }
            if (publisherDrop != null)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.IsPublisherDrop == publisherDrop.Value);
            }
            if (twitchDrop != null)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.IsTwitchDrop == twitchDrop.Value);
            }
            if (lootCrateDrop != null)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.IsLootCrateDrop == lootCrateDrop.Value);
            }
            if (craftable != null)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.IsCraftable == craftable.Value);
            }
            if (manipulated != null)
            {
                query = query.Where(x => id.Contains(x.ClassId) || (x.MarketItem != null && x.MarketItem.IsBeingManipulated == manipulated.Value));
            }

            // Join
            query = query
                .Include(x => x.App)
                .Include(x => x.CreatorProfile)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .Include(x => x.StoreItem).ThenInclude(x => x.Stores).ThenInclude(x => x.Store)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency);

            // Sort
            if (!String.IsNullOrEmpty(sortBy))
            {
                if (sortBy.Contains("MarketItem"))
                {
                    query = query.Where(x => x.MarketItem != null);
                    if (sortBy.Contains("Price"))
                    {
                        query = query.Where(x => x.MarketItem.SellOrderLowestPrice > 0);
                    }
                }
                if (sortBy.Contains("StoreItem"))
                {
                    query = query.Where(x => x.StoreItem != null);
                    if (sortBy.Contains("Price"))
                    {
                        query = query.Where(x => x.StoreItem.Price > 0);
                    }
                }
                if (String.Equals(sortBy, "PriceMovement", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.OrderByDirection(x => (x.MarketItem.SellOrderLowestPrice - x.StoreItem.Price) ?? 0, sortDirection);
                }
                else
                {
                    query = query.SortBy(sortBy, sortDirection);
                }
            }
            else
            {
                query = query.OrderByDescending(x => x.TimeAccepted ?? x.TimeCreated);
            }

            // Paginate
            count = Math.Max(0, Math.Min(5000, count));
            return Ok(!detailed
                ? await query.PaginateAsync(start, count, x => _mapper.Map<SteamAssetDescription, ItemDescriptionWithPriceDTO>(x, this))
                : await query.PaginateAsync(start, count, x => _mapper.Map<SteamAssetDescription, ItemDetailedDTO>(x, this))
            );
        }

        /// <summary>
        /// Get item information
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="id">Item GUID, ID64, or name</param>
        /// <response code="200">The item details.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="404">If the request item cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ItemDetailedDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItem([FromRoute] string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Item id is invalid");
            }

            var guid = Guid.Empty;
            var id64 = (ulong)0;
            Guid.TryParse(id, out guid);
            ulong.TryParse(id, out id64);

            var appId = this.App().Guid;
            var item = await _db.SteamAssetDescriptions
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.CreatorProfile)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .Include(x => x.StoreItem).ThenInclude(x => x.Stores).ThenInclude(x => x.Store)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .Where(x => x.AppId == appId)
                .FirstOrDefaultAsync(x =>
                    (guid != Guid.Empty && x.Id == guid) ||
                    (id64 > 0 && x.ClassId == id64) ||
                    (x.Name == id)
                );

            var itemDetails = _mapper.Map<SteamAssetDescription, ItemDetailedDTO>(item, this);
            if (itemDetails == null)
            {
                return NotFound($"Item was not found");
            }

            // Calculate market price ranks
            if (!String.IsNullOrEmpty(itemDetails?.ItemType))
            {
                var thisItemCheapestPrice = (itemDetails.IsAvailableOnStore
                    ? (itemDetails.StorePrice > 0 && itemDetails.MarketSellOrderLowestPrice > 0) ? Math.Min(itemDetails.StorePrice.Value, itemDetails.MarketSellOrderLowestPrice.Value) : itemDetails.StorePrice
                    : itemDetails.MarketSellOrderLowestPrice
                );
                var otherItems = await _db.SteamAssetDescriptions
                    .AsNoTracking()
                    .Where(x => x.ItemType == itemDetails.ItemType)
                    .Where(x => x.IsMarketable || x.MarketableRestrictionDays > 0)
                    .Select(x => new
                    {
                        MarketPrice = (x.MarketItem != null ? x.MarketItem.SellOrderLowestPrice : 0),
                        MarketCurrency = (x.MarketItem != null ? x.MarketItem.Currency : null),
                        StorePrices = (x.StoreItem != null && x.StoreItem.IsAvailable ? x.StoreItem.Prices : null)
                    })
                    .ToListAsync();

                var otherCheaperItems = otherItems.Where(x =>
                    (x.MarketPrice > 0 && this.Currency().CalculateExchange(x.MarketPrice, x.MarketCurrency) < thisItemCheapestPrice) ||
                    (x.StorePrices != null && x.StorePrices.ContainsKey(this.Currency().Name) && x.StorePrices[this.Currency().Name] < thisItemCheapestPrice)
                );

                itemDetails.MarketRankIndex = otherCheaperItems.Count();
                itemDetails.MarketRankTotal = otherItems.Count();
            }

            return Ok(itemDetails);
        }

        /// <summary>
        /// List item sell orders
        /// </summary>
        /// <param name="id">Item GUID, ID64, or name</param>
        /// <param name="start">Return item sell orders starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 100.</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="404">If the request item cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/sellOrders")]
        [ProducesResponseType(typeof(PaginatedResult<ItemOrderDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemSellOrders([FromRoute] string id, [FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Item id is invalid");
            }

            var guid = Guid.Empty;
            var id64 = (ulong)0;
            Guid.TryParse(id, out guid);
            ulong.TryParse(id, out id64);

            var appId = this.App().Guid;
            var item = await _db.SteamMarketItems
                .AsNoTracking()
                .Where(x => x.AppId == appId)
                .FirstOrDefaultAsync(x =>
                    (guid != Guid.Empty && x.Description.Id == guid) ||
                    (id64 > 0 && x.Description.ClassId == id64) ||
                    (x.Description.Name == id)
                );
            if (item == null)
            {
                return NotFound($"Item was not found");
            }

            var query = _db.SteamMarketItemSellOrder
                .Include(x => x.Item.Currency)
                .Where(x => x.ItemId == item.Id)
                .OrderBy(x => x.Price);

            count = Math.Max(0, Math.Min(100, count));
            return Ok(
                await query.PaginateAsync(start, count, x => _mapper.Map<SteamMarketItemOrder, ItemOrderDTO>(x, this))
            );
        }

        /// <summary>
        /// List item buy orders
        /// </summary>
        /// <param name="id">Item GUID, ID64, or name</param>
        /// <param name="start">Return item buy orders starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 100.</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="404">If the request item cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/buyOrders")]
        [ProducesResponseType(typeof(PaginatedResult<ItemOrderDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemBuyOrders([FromRoute] string id, [FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Item id is invalid");
            }

            var guid = Guid.Empty;
            var id64 = (ulong)0;
            Guid.TryParse(id, out guid);
            ulong.TryParse(id, out id64);

            var appId = this.App().Guid;
            var item = await _db.SteamMarketItems
                .AsNoTracking()
                .Where(x => x.AppId == appId)
                .FirstOrDefaultAsync(x =>
                    (guid != Guid.Empty && x.Description.Id == guid) ||
                    (id64 > 0 && x.Description.ClassId == id64) ||
                    (x.Description.Name == id)
                );
            if (item == null)
            {
                return NotFound($"Item was not found");
            }

            var query = _db.SteamMarketItemBuyOrder
                .Include(x => x.Item.Currency)
                .Where(x => x.ItemId == item.Id)
                .OrderByDescending(x => x.Price);

            count = Math.Max(0, Math.Min(100, count));
            return Ok(
                await query.PaginateAsync(start, count, x => _mapper.Map<SteamMarketItemOrder, ItemOrderDTO>(x, this))
            );
        }

        /// <summary>
        /// Get item sales history chart data, grouped by day (UTC)
        /// </summary>
        /// <remarks>
        /// Detailed financial details (high/low/open/close) are only reported for periods less than 30 days.
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="id">Item GUID, ID64, or name</param>
        /// <param name="maxDays">The maximum number of days worth of sales history to return. Use <code>-1</code> for all sales history</param>
        /// <response code="200">List of item sales per day grouped/keyed by UTC date.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="404">If the request item cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/sales")]
        [ProducesResponseType(typeof(IEnumerable<ItemSalesChartPointDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemSales([FromRoute] string id, int maxDays = 30)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Item id is invalid");
            }

            var guid = Guid.Empty;
            var id64 = (ulong)0;
            Guid.TryParse(id, out guid);
            ulong.TryParse(id, out id64);

            var appId = this.App().Guid;
            var item = await _db.SteamMarketItems
                .AsNoTracking()
                .Where(x => x.AppId == appId)
                .FirstOrDefaultAsync(x =>
                    (guid != Guid.Empty && x.Description.Id == guid) ||
                    (id64 > 0 && x.Description.ClassId == id64) ||
                    (x.Description.Name == id)
                );
            if (item == null)
            {
                return NotFound($"Item was not found");
            }

            var maxDaysCutoff = (maxDays >= 1 ? DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(maxDays)) : (DateTimeOffset?)null);
            var getCandleData = (maxDays >= 1 && maxDays <= 30);
            var query = _db.SteamMarketItemSale
                .AsNoTracking()
                .Where(x => x.ItemId == item.Id)
                .Where(x => maxDaysCutoff == null || x.Timestamp.Date >= maxDaysCutoff.Value.Date)
                .GroupBy(x => x.Timestamp.Date)
                .OrderBy(x => x.Key.Date)
                .Select(x => new
                {
                    Date = x.Key,
                    Median = x.Average(y => y.MedianPrice),
                    High = (getCandleData ? x.Max(y => y.MedianPrice) : 0),
                    Low = (getCandleData ? x.Min(y => y.MedianPrice) : 0),
                    Open = (getCandleData && x.Count() > 0 ? x.OrderBy(y => y.Timestamp).FirstOrDefault().MedianPrice : 0),
                    Close = (getCandleData && x.Count() > 0 ? x.OrderBy(y => y.Timestamp).LastOrDefault().MedianPrice : 0),
                    Volume = x.Sum(y => y.Quantity)
                });

            var sales = (await query.ToListAsync()).Select(
                x => new ItemSalesChartPointDTO
                {
                    Date = x.Date,
                    Median = this.Currency().ToPrice(this.Currency().CalculateExchange((long)Math.Round(x.Median, 0))),
                    High = this.Currency().ToPrice(this.Currency().CalculateExchange((long)x.High)),
                    Low = this.Currency().ToPrice(this.Currency().CalculateExchange((long)x.Low)),
                    Open = this.Currency().ToPrice(this.Currency().CalculateExchange((long)x.Open)),
                    Close = this.Currency().ToPrice(this.Currency().CalculateExchange((long)x.Close)),
                    Volume = x.Volume
                }
            );

            return Ok(sales.ToArray());
        }

        /// <summary>
        /// Get list of top users holding an item
        /// </summary>
        /// <param name="id">Item GUID, ID64, or name</param>
        /// <param name="max">The maximum number of users to return.</param>
        /// <response code="200">List of top user holding the item.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="404">If the request item cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/topHolders")]
        [ProducesResponseType(typeof(IEnumerable<ItemHoldingUserDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemTopHolders([FromRoute] string id, int max = 30)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Item id is invalid");
            }

            max = Math.Max(0, Math.Min(PaginationExtensions.MaxCount, max));
            if (max <= 0)
            {
                return BadRequest("Max must be greater than zero");
            }

            var guid = Guid.Empty;
            var id64 = (ulong)0;
            Guid.TryParse(id, out guid);
            ulong.TryParse(id, out id64);

            var appId = this.App().Guid;
            var item = await _db.SteamAssetDescriptions
                .AsNoTracking()
                .Where(x => x.AppId == appId)
                .FirstOrDefaultAsync(x =>
                    (guid != Guid.Empty && x.Id == guid) ||
                    (id64 > 0 && x.ClassId == id64) ||
                    (x.Name == id)
                );
            if (item == null)
            {
                return NotFound($"Item was not found");
            }

            var topHoldingProfiles = await _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.DescriptionId == item.Id)
                .GroupBy(x => x.ProfileId)
                .Select(x => new
                {
                    Profile = _db.SteamProfiles.FirstOrDefault(p => p.Id == x.Key),
                    Items = x.Sum(y => y.Quantity)
                })
                .OrderByDescending(x => x.Items)
                .Take(max)
                .ToListAsync();

            return Ok(
                topHoldingProfiles.Select(x => new ItemHoldingUserDTO
                {
                    SteamId = (x.Profile.ItemAnalyticsParticipation == ItemAnalyticsParticipationType.Public) ? x.Profile.SteamId : null,
                    Name = (x.Profile.ItemAnalyticsParticipation == ItemAnalyticsParticipationType.Public) ? x.Profile.Name : null,
                    AvatarUrl = (x.Profile.ItemAnalyticsParticipation == ItemAnalyticsParticipationType.Public) ? x.Profile.AvatarUrl : null,
                    Items = x.Items
                }).ToArray()
            );
        }

        /// <summary>
        /// Get demand statistics for all items of the specified type
        /// </summary>
        /// <param name="type">The item type</param>=
        /// <returns>The demand statistics for the item type</returns>
        /// <remarks>
        /// Response is cached for 12hrs.
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <response code="200">The demand statistics for the item type.</response>
        /// <response code="400">If the item type is missing.</response>
        /// <response code="404">If the item type doesn't exist (or doesn't contain any marketable items).</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("type/{type}/demand")]
        [ProducesResponseType(typeof(ItemGroupDemandDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OutputCache(Duration = (12 * 60 * 60 /* 12hr */))]
        public async Task<IActionResult> GetItemDemandByItemType([FromRoute] string type)
        {
            var appId = this.App().Guid;

            if (string.IsNullOrEmpty(type))
            {
                return BadRequest("Item type is missing");
            }

            var currency = await _db.SteamCurrencies.AsNoTracking().FirstOrDefaultAsync(x => x.Name == Constants.SteamDefaultCurrency);
            var items = await _db.SteamAssetDescriptions.AsNoTracking()
                .Where(x => x.AppId == appId)
                .Where(x => x.IsAccepted == true)
                .Where(x => x.ItemType == type)
                .Include(x => x.App)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .OrderByDescending(x => x.TimeAccepted ?? x.TimeCreated)
                .ToListAsync();
            if (items?.Any() != true)
            {
                return NotFound("No items found for item type");
            }

            return Ok(new ItemGroupDemandDTO
            {
                TotalItems = items.Count,
                Last24hrMedianPrice = this.Currency().CalculateExchange(items.Where(x => x.IsMarketable && x.MarketItem != null).Median(x => x.MarketItem.Stable24hrSellOrderLowestPrice), currency),
                Last168hrMedianPrice = this.Currency().CalculateExchange(items.Where(x => x.IsMarketable && x.MarketItem != null).Median(x => x.MarketItem.Last168hrValue), currency),
                Last24hrMedianMovementFromStorePrice = this.Currency().CalculateExchange(items.Where(x => x.IsMarketable && x.MarketItem != null && x.MarketItem.Stable24hrSellOrderLowestPrice > 0 && x.StoreItem != null && x.StoreItem.Price > 0).Median(x => x.MarketItem.Stable24hrSellOrderLowestPrice - x.StoreItem.Price.Value), currency),
                Last168hrMedianMovementFromStorePrice = this.Currency().CalculateExchange(items.Where(x => x.IsMarketable && x.MarketItem != null && x.MarketItem.Last168hrValue > 0 && x.StoreItem != null && x.StoreItem.Price > 0).Median(x => x.MarketItem.Last168hrValue - x.StoreItem.Price.Value), currency),
                TotalMarketSupply = items.Where(x => x.IsMarketable && x.MarketItem != null).Sum(x => x.MarketItem.SellOrderCount),
                MedianMarketSupply = items.Where(x => x.IsMarketable && x.MarketItem != null).Median(x => x.MarketItem.SellOrderCount),
                TotalMarketDemand = items.Where(x => x.IsMarketable && x.MarketItem != null).Sum(x => x.MarketItem.Last24hrSales),
                MedianMarketDemand = items.Where(x => x.IsMarketable && x.MarketItem != null).Median(x => x.MarketItem.Last24hrSales),
            });
        }

        /// <summary>
        /// Get all items that belong to the specified collection
        /// </summary>
        /// <param name="name">The name of the item collection</param>
        /// <param name="creatorId">Optional creator id to filter against. Useful when there are multiple collections of the same name by different creators</param>
        /// <returns>The items belonging to the collection</returns>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <response code="200">The item collection details.</response>
        /// <response code="400">If the collection name is missing.</response>
        /// <response code="404">If the collection name doesn't exist (or doesn't contain any marketable items).</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("collection/{name}")]
        [ProducesResponseType(typeof(ItemCollectionDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsByCollection([FromRoute] string name, [FromQuery] ulong? creatorId = null)
        {
            var appId = this.App().Guid;

            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Collection name is missing");
            }

            var acceptedAssetDescriptions = await _db.SteamAssetDescriptions.AsNoTracking()
                .Where(x => x.AppId == appId)
                .Where(x => x.IsAccepted == true)
                .Where(x => x.ItemCollection == name)
                .Where(x => creatorId == null || x.CreatorId == creatorId || (x.CreatorProfile != null && x.CreatorProfile.SteamId == creatorId.ToString()) || (x.App != null && x.App.SteamId == creatorId.ToString()))
                .Include(x => x.App)
                .Include(x => x.CreatorProfile)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .Include(x => x.StoreItem).ThenInclude(x => x.Stores).ThenInclude(x => x.Store)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .OrderByDescending(x => x.TimeAccepted ?? x.TimeCreated)
                .ToListAsync();
            if (acceptedAssetDescriptions?.Any() != true)
            {
                return NotFound("Collection does not exist");
            }

            var unacceptedWorkshopFiles = await _db.SteamWorkshopFiles.AsNoTracking()
                .Where(x => x.AppId == appId)
                .Where(x => x.IsAccepted == false)
                .Where(x => x.ItemCollection == name)
                .Where(x => x.DescriptionId == null)
                .Where(x => creatorId == null || x.CreatorId == creatorId || (x.CreatorProfile != null && x.CreatorProfile.SteamId == creatorId.ToString()) || (x.App != null && x.App.SteamId == creatorId.ToString()))
                .Include(x => x.App)
                .Include(x => x.CreatorProfile)
                .OrderByDescending(x => x.TimeAccepted ?? x.TimeCreated)
                .ToListAsync();

            var creator = acceptedAssetDescriptions
                .Where(x => x.CreatorProfile != null)
                .GroupBy(x => x.CreatorProfile)
                .FirstOrDefault();

            return Ok(new ItemCollectionDTO()
            {
                Name = name,
                CreatorName = creator?.Key.Name,
                CreatorAvatarUrl = creator?.Key.AvatarUrl,
                BuyNowPrice = acceptedAssetDescriptions.Sum(x => x.GetCheapestBuyPrice(this.Currency())?.Price ?? 0),
                AcceptedItems = _mapper.Map<SteamAssetDescription, ItemDescriptionWithPriceDTO>(acceptedAssetDescriptions, this)?.ToArray(),
                UnacceptedItems = _mapper.Map<SteamWorkshopFile, ItemDescriptionWithActionsDTO>(unacceptedWorkshopFiles, this)?.ToArray()
            });
        }

        /// <summary>
        /// List all known item definition archives
        /// </summary>
        /// <returns>List of item definition archives</returns>
        /// <response code="200">List of known item definition archives.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("definitionArchives")]
        [ProducesResponseType(typeof(IEnumerable<ItemDefinitionArchiveIdentifierDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemDefinitionArchives()
        {
            var appId = this.App().Guid;

            var itemDefinitionArchives = await _db.SteamItemDefinitionsArchive.AsNoTracking()
                .Where(x => x.AppId == appId)
                .OrderByDescending(x => x.TimePublished)
                .Select(x => new ItemDefinitionArchiveIdentifierDTO()
                {
                    Id = x.Id.ToString(),
                    Digest = x.Digest,
                    Size = x.ItemDefinitionsSize,
                    ItemCount = x.ItemDefinitionsCount,
                    Timestamp = x.TimePublished
                })
                .OrderByDescending(x => x.Timestamp)
                .ToArrayAsync();

            return Ok(itemDefinitionArchives);
        }

        /// <summary>
        /// Get item definition archive contents
        /// </summary>
        /// <param name="digest">The digest of the item definition archive to get</param>
        /// <param name="raw">If true, the raw archive content is returned as a string. If false, the archive is returned as a list of  <see cref="ItemDefinition"/> </param>
        /// <returns>The item definition archive contents</returns>
        /// <response code="200">The item definition archive contents.</response>
        /// <response code="404">If the item definition archive doesn't exist.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("definitionArchive/{digest}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ItemDefinition[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemDefinitionArchive([FromRoute] string digest, [FromQuery] bool raw = false)
        {
            var appId = this.App().Guid;

            var itemDefinitionArchive = await _db.SteamItemDefinitionsArchive.AsNoTracking()
                .Where(x => x.AppId == appId)
                .Where(x => x.Digest == digest)
                .Select(x => x.ItemDefinitions)
                .FirstOrDefaultAsync();
            if (String.IsNullOrEmpty(itemDefinitionArchive))
            {
                return NotFound("Item definition archive with that digest does not exist");
            }

            return Ok(raw
                ? itemDefinitionArchive
                : JsonSerializer.Deserialize<ItemDefinition[]>(itemDefinitionArchive)
            );
        }

        /// <summary>
        /// List all known item filters
        /// </summary>
        /// <remarks>Response is cached for 24hrs</remarks>
        /// <response code="200">List of unique item filters</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("filters")]
        [ProducesResponseType(typeof(IEnumerable<ItemFilterDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OutputCache(Duration = (24 * 60 * 60 /* 24hr */))]
        public async Task<IActionResult> GetItemFilters()
        {
            var appId = this.App().Guid;
            var itemFilters = await _db.SteamApps.AsNoTracking()
                .Where(x => x.Id == appId)
                .SelectMany(x => x.AssetFilters)
                .ToArrayAsync();

            return Ok(
                _mapper.Map<SteamAssetFilter, ItemFilterDTO>(itemFilters, this)
            );
        }

        /// <summary>
        /// List all known item types
        /// </summary>
        /// <remarks>Response is cached for 24hrs</remarks>
        /// <response code="200">List of unique item types</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("types")]
        [ProducesResponseType(typeof(IEnumerable<ItemTypeGroupDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OutputCache(Duration = (24 * 60 * 60 /* 24hr */))]
        public async Task<IActionResult> GetItemTypes()
        {
            var appId = this.App().Guid;
            var itemTypes = await _db.SteamAssetDescriptions.AsNoTracking()
                .Where(x => x.AppId == appId)
                .Where(x => !String.IsNullOrEmpty(x.ItemType))
                .GroupBy(x => x.ItemType)
                .Select(x => x.Key)
                .ToArrayAsync();

            // TODO: Add support for other apps
            return Ok(
                itemTypes
                    .GroupBy(x => x.RustItemTypeGroup())
                    .OrderBy(x => x.Key)
                    .Select(g => new ItemTypeGroupDTO()
                    {
                        Name = g.Key,
                        ItemTypes = g.OrderBy(x => x).Select(i => new ItemTypeDTO()
                        {
                            Id = i.RustItemTypeToShortName(),
                            Name = i
                        }).ToArray()
                    })
                    .ToArray()
            );
        }

        /// <summary>
        /// List the price(s) of all items
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="markets">If specified, only item prices from these markets will be returned.</param>
        /// <response code="200">If <paramref name="markets"/> is <code>non-null</code>, the response will be a list of <see cref="ItemMarketPricesDTO"/>. If <code>null</code>, the response will be a list of <see cref="ItemBestMarketPriceDTO"/></response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("prices")]
        [ProducesResponseType(typeof(IEnumerable<ItemBestMarketPriceDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IEnumerable<ItemMarketPricesDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemPrices([FromQuery] MarketType[] markets = null)
        {
            var appId = this.App().Guid;
            var query = (IQueryable<SteamAssetDescription>)_db.SteamAssetDescriptions
                .AsNoTracking()
                .Where(x => x.AppId == appId && x.ClassId > 0)
                .Include(x => x.App);

            markets ??= new MarketType[0];
            if (markets.Length == 0 || markets.Contains(MarketType.SteamStore))
            {
                query = query.Include(x => x.StoreItem).ThenInclude(x => x.Currency);
            }
            if (markets.Length == 0 || markets.Any(x => x >= MarketType.SteamCommunityMarket))
            {
                query = query.Include(x => x.MarketItem).ThenInclude(x => x.Currency);
            }

            var items = await query
                .OrderBy(x => x.ClassId)
                .ToArrayAsync();

            if (markets.Length > 0)
            {
                foreach (var item in items.Where(x => x.MarketItem != null))
                {
                    item.MarketItem.BuyPrices = new Steam.Data.Store.Types.PersistableMarketPriceDictionary(
                        item.MarketItem.BuyPrices.Where(x => markets.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value)
                    );
                }
            }

            return Ok(markets.Length > 0
                ? _mapper.Map<SteamAssetDescription, ItemMarketPricesDTO>(items, this)
                : _mapper.Map<SteamAssetDescription, ItemBestMarketPriceDTO>(items, this)
            );
        }
    }
}
