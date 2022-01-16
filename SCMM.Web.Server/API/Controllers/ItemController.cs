using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Extensions;
using SCMM.Steam.API;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Server.Extensions;

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
        /// <param name="tradeable">If <code>true</code>, only tradable items are returned</param>
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
                                                  [FromQuery] bool glow = false, [FromQuery] bool glowsight = false, [FromQuery] bool cutout = false, [FromQuery] bool marketable = false, [FromQuery] bool tradeable = false,
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

            var query = _db.SteamAssetDescriptions.AsNoTracking();

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
                query = query.Where(x => id.Contains(x.ClassId) || x.CreatorId == creatorId || (x.CreatorId == null && x.App.SteamId == creatorId.ToString()));
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
            if (tradeable)
            {
                query = query.Where(x => id.Contains(x.ClassId) || x.IsTradable == true);
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

            query = query
                .Include(x => x.App)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .OrderByDescending(x => x.TimeAccepted ?? x.TimeCreated);

            // TODO: Sorting...

            // Paginate and return
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

            var item = await _db.SteamAssetDescriptions
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.CreatorProfile)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .Include(x => x.StoreItem).ThenInclude(x => x.Stores).ThenInclude(x => x.Store)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .FirstOrDefaultAsync(x =>
                    (guid != Guid.Empty && x.Id == guid) ||
                    (id64 > 0 && x.ClassId == id64) ||
                    (x.Name == id)
                );

            if (item == null)
            {
                return NotFound($"Item was not found");
            }

            return Ok(
                _mapper.Map<SteamAssetDescription, ItemDetailedDTO>(item, this)
            );
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

            var query = _db.SteamMarketItemSellOrder
                .Include(x => x.Item.Currency)
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

            var query = _db.SteamMarketItemBuyOrder
                .Include(x => x.Item.Currency)
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
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <response code="200">List of item sales per day grouped/keyed by UTC date.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/salesHistory")]
        [ProducesResponseType(typeof(IEnumerable<ItemSaleChartDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemSalesTimeline([FromRoute] string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Item id is invalid");
            }

            var guid = Guid.Empty;
            var id64 = (ulong)0;
            Guid.TryParse(id, out guid);
            ulong.TryParse(id, out id64);

            var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var query = _db.SteamMarketItemSale
                .AsNoTracking()
                .Where(x =>
                    (guid != Guid.Empty && x.Item.Description.Id == guid) ||
                    (id64 > 0 && x.Item.Description.ClassId == id64) ||
                    (x.Item.Description.Name == id)
                )
                .Where(x => x.Timestamp.Date <= yesterday.Date)
                .GroupBy(x => x.Timestamp.Date)
                .OrderBy(x => x.Key.Date)
                .Select(x => new
                {
                    Date = x.Key,
                    MedianPrice = x.Average(y => y.MedianPrice),
                    Quantity = x.Sum(y => y.Quantity)
                });

            var sales = (await query.ToListAsync()).Select(
                x => new ItemSaleChartDTO
                {
                    Date = x.Date,
                    Price = this.Currency().ToPrice(this.Currency().CalculateExchange((long)Math.Round(x.MedianPrice, 0))),
                    Quantity = x.Quantity
                }
            );

            return Ok(sales);
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
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Collection name is missing");
            }

            var assetDescriptions = await _db.SteamAssetDescriptions.AsNoTracking()
                .Where(x => x.ItemCollection == name)
                .Where(x => creatorId == null || x.CreatorId == creatorId || (x.CreatorId == null && x.App.SteamId == creatorId.ToString()))
                .Include(x => x.App)
                .Include(x => x.CreatorProfile)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .OrderByDescending(x => x.TimeAccepted ?? x.TimeCreated)
                .ToListAsync();

            if (assetDescriptions?.Any() != true)
            {
                return NotFound("Collection does not exist");
            }

            var assetDetails = _mapper.Map<List<SteamAssetDescription>, ItemCollectionDTO>(assetDescriptions, this);
            return Ok(assetDetails);
        }

        /// <summary>
        /// List all known item types
        /// </summary>
        /// <response code="200">List of unique item types</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("types")]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemTypes()
        {
            return Ok(
                await _db.SteamAssetDescriptions.AsNoTracking()
                    .Where(x => !String.IsNullOrEmpty(x.ItemType))
                    .GroupBy(x => x.ItemType)
                    .Select(x => x.Key)
                    .ToArrayAsync()
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
            var items = await _db.SteamAssetDescriptions
                .Include(x => x.App)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .OrderBy(x => x.ClassId)
                .ToArrayAsync();

            return Ok(
                _mapper.Map<SteamAssetDescription, ItemMarketPricingDTO>(items, this)
            );
        }
    }
}
