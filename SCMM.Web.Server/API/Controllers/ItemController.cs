using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Extensions;
using SCMM.Steam.API;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using SCMM.Web.Data.Models;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Data.Models.Extensions;
using SCMM.Web.Server.Extensions;
using SCMM.Steam.Data.Models.Enums;

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

        private readonly SteamService _steam;

        public ItemController(ILogger<ItemController> logger, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper, SteamService steam)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
            _steam = steam;
        }

        /// <summary>
        /// Search for items
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="id">Optional array of ids to be included in the list. Helpful when you want search for items and compare against a known (but unrelated) item</param>
        /// <param name="filter">Optional search filter. Matches against item GUID, ID64, name, description, author, type, collection, and tags</param>
        /// <param name="type">If specified, only items matching the supplied item type are returned</param>
        /// <param name="collection">If specified, only items matching the supplied item collection are returned</param>
        /// <param name="creatorId">If specified, only items published by the supplied creator id are returned</param>
        /// <param name="glow">If <code>true</code>, only items tagged with 'glow' are returned</param>
        /// <param name="glowsight">If <code>true</code>, only items tagged with 'glowsight' are returned</param>
        /// <param name="cutout">If <code>true</code>, only items tagged with 'cutout' are returned</param>
        /// <param name="marketable">If <code>true</code>, only marketable items are returned</param>
        /// <param name="tradable">If <code>true</code>, only tradable items are returned</param>
        /// <param name="returning">If <code>true</code>, only items that have been released over multiple stores are returned</param>
        /// <param name="banned">If <code>true</code>, only banned items are returned</param>
        /// <param name="specialDrop">If <code>true</code>, only special drops are returned</param>
        /// <param name="twitchDrop">If <code>true</code>, only twitch drops are returned</param>
        /// <param name="craftable">If <code>true</code>, only craftable items are returned</param>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Use -1 to return all items</param>
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
        public async Task<IActionResult> GetItems([FromQuery] ulong?[] id = null, [FromQuery] string filter = null, [FromQuery] string type = null, [FromQuery] string collection = null, [FromQuery] ulong? creatorId = null,
                                                  [FromQuery] bool glow = false, [FromQuery] bool glowsight = false, [FromQuery] bool cutout = false, [FromQuery] bool marketable = false, [FromQuery] bool tradable = false,
                                                  [FromQuery] bool returning = false, [FromQuery] bool banned = false, [FromQuery] bool specialDrop = false, [FromQuery] bool twitchDrop = false, [FromQuery] bool craftable = false,
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

            // Filter app
            var appId = this.App().Guid;
            var query = _db.SteamAssetDescriptions.AsNoTracking()
                .Where(x => x.AppId == appId);

            // Filter search
            filter = Uri.UnescapeDataString(filter?.Trim() ?? string.Empty);
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

            // Filter toggles
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.ItemType == type);
            }
            if (!string.IsNullOrEmpty(collection))
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.ItemCollection == collection);
            }
            if (creatorId != null)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.CreatorId == creatorId || (x.CreatorProfile != null && x.CreatorProfile.SteamId == creatorId.ToString()));
            }
            if (glow)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.HasGlow == true);
            }
            if (glowsight)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.HasGlowSights == true);
            }
            if (cutout)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.HasCutout == true);
            }
            if (marketable)
            {
                query = query.Where(x => id.Contains(x.ClassId) || (x.IsMarketable == true || x.MarketableRestrictionDays > 0));
            }
            if (tradable)
            {
                query = query.Where(x => id.Contains(x.ClassId) || (x.IsTradable == true || x.TradableRestrictionDays > 0 || (x.IsTradable && x.TradableRestrictionDays == null)));
            }
            if (returning)
            {
                query = query.Where(x => id.Contains(x.ClassId) || (x.StoreItem != null && x.StoreItem.HasReturnedToStore));
            }
            if (banned)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.IsBanned == true);
            }
            if (specialDrop)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.IsSpecialDrop == true);
            }
            if (twitchDrop)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.IsTwitchDrop == true);
            }
            if (craftable)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.IsCraftable == true);
            }
            
            // Join
            query = query
                .Include(x => x.App)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .Include(x => x.StoreItem).ThenInclude(x => x.Stores).ThenInclude(x => x.Store)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency);

            // Sort
            if (!String.IsNullOrEmpty(sortBy))
            {
                query = query.SortBy(sortBy, sortDirection);
            }
            else
            {
                query = query.OrderByDescending(x => x.TimeAccepted ?? x.TimeCreated);
            }

            // Paginate
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
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
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
            var query = _db.SteamMarketItemSellOrder
                .Include(x => x.Item.Currency)
                .Where(x => x.Item.AppId == appId)
                .Where(x =>
                    (guid != Guid.Empty && x.Item.Description.Id == guid) ||
                    (id64 > 0 && x.Item.Description.ClassId == id64) ||
                    (x.Item.Description.Name == id)
                )
                .OrderBy(x => x.Price);

            return Ok(
                await query.PaginateAsync(start, count, x => _mapper.Map<SteamMarketItemOrder, ItemOrderDTO>(x, this))
            );
        }

        /// <summary>
        /// List item buy orders
        /// </summary>
        /// <param name="id">Item GUID, ID64, or name</param>
        /// <param name="start">Return item buy orders starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
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
            var query = _db.SteamMarketItemBuyOrder
                .Include(x => x.Item.Currency)
                .Where(x => x.Item.AppId == appId)
                .Where(x =>
                    (guid != Guid.Empty && x.Item.Description.Id == guid) ||
                    (id64 > 0 && x.Item.Description.ClassId == id64) ||
                    (x.Item.Description.Name == id)
                )
                .OrderByDescending(x => x.Price);

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
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/sales")]
        [ProducesResponseType(typeof(IEnumerable<ItemSalesChartPointDTO>), StatusCodes.Status200OK)]
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

            var maxDaysCutoff = (maxDays >= 1 ? DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(maxDays)) : (DateTimeOffset?)null);
            var getCandleData = (maxDays >= 1 && maxDays <= 30);
            var appId = this.App().Guid;
            var query = _db.SteamMarketItemSale
                .AsNoTracking()
                .Where(x => x.Item.AppId == appId)
                .Where(x =>
                    (guid != Guid.Empty && x.Item.Description.Id == guid) ||
                    (id64 > 0 && x.Item.Description.ClassId == id64) ||
                    (x.Item.Description.Name == id)
                )
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

            return Ok(sales);
        }

        /// <summary>
        /// Get list of top users holding an item
        /// </summary>
        /// <param name="id">Item GUID, ID64, or name</param>
        /// <param name="max">The maximum number of users to return.</param>
        /// <response code="200">List of top user holding the item.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/topHolders")]
        [ProducesResponseType(typeof(IEnumerable<ItemHoldingUserDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemTopHolders([FromRoute] string id, int max = 30)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Item id is invalid");
            }

            max = Math.Max(max, 0);
            if (max <= 0)
            {
                return BadRequest("Max must be greater than zero");
            }

            var guid = Guid.Empty;
            var id64 = (ulong)0;
            Guid.TryParse(id, out guid);
            ulong.TryParse(id, out id64);

            var appId = this.App().Guid;
            var topHoldingProfiles = await _db.SteamProfileInventoryItems
                .AsNoTracking()
                .Where(x => x.AppId == appId)
                .Where(x => x.Profile.ItemAnalyticsParticipation != ItemAnalyticsParticipationType.Private)
                .Where(x =>
                    (guid != Guid.Empty && x.Description.Id == guid) ||
                    (id64 > 0 && x.Description.ClassId == id64) ||
                    (x.Description.Name == id)
                )
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
                })
            );
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
                .Where(x => creatorId == null || x.CreatorId == creatorId || (x.CreatorProfile != null && x.CreatorProfile.SteamId == creatorId.ToString()))
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
                .Where(x => creatorId == null || x.CreatorId == creatorId || (x.CreatorProfile != null && x.CreatorProfile.SteamId == creatorId.ToString()))
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
                BuyNowPrice = acceptedAssetDescriptions.Sum(x => x.GetCheapestBuyPrice(this.Currency()).Price),
                AcceptedItems = _mapper.Map<SteamAssetDescription, ItemDescriptionWithPriceDTO>(acceptedAssetDescriptions, this),
                UnacceptedItems = _mapper.Map<SteamWorkshopFile, ItemDescriptionWithActionsDTO>(unacceptedWorkshopFiles, this)
            });
        }

        /// <summary>
        /// List all known item types
        /// </summary>
        /// <response code="200">List of unique item types</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("types")]
        [ProducesResponseType(typeof(IEnumerable<ItemTypeGroupDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
                itemTypes.GroupBy(x => x.ToRustItemGroup()).OrderBy(x => x.Key).Select(g => new ItemTypeGroupDTO()
                { 
                    Name = g.Key,
                    ItemTypes = g.OrderBy(x => x).Select(i => new ItemTypeDTO()
                    {
                        Id = i.ToRustItemShortName(),
                        Name = i
                    })
                })
            );
        }

        /// <summary>
        /// List all item prices across all known markets
        /// </summary>
        /// <response code="200">List of item prices</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("prices")]
        [ProducesResponseType(typeof(IEnumerable<ItemMarketPricingDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemPrices()
        {
            var appId = this.App().Guid;
            var items = await _db.SteamAssetDescriptions
                .Include(x => x.App)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .Where(x => x.AppId == appId)
                .OrderBy(x => x.ClassId)
                .ToArrayAsync();

            return Ok(
                _mapper.Map<SteamAssetDescription, ItemMarketPricingDTO>(items, this)
            );
        }
    }
}
