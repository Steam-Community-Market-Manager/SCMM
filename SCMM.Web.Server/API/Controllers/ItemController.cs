using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Store.Extensions;
using SCMM.Steam.API;
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
        /// List all known items
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="filter">Optional search filter. Matches against item GUID, ID64, name, description, author, type, collection, and tags</param>
        /// <param name="itemType">If specified, only items matching the supplied item type are returned</param>
        /// <param name="glow">If <code>true</code>, only items tagged with 'glow' are returned</param>
        /// <param name="glowsight">If <code>true</code>, only items tagged with 'glowsight' are returned</param>
        /// <param name="cutout">If <code>true</code>, only items tagged with 'cutout' are returned</param>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
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
        public async Task<IActionResult> GetItems([FromQuery] string filter = null, [FromQuery] string itemType = null, [FromQuery] bool glow = false, [FromQuery] bool glowsight = false, [FromQuery] bool cutout = false,
                                                  [FromQuery] int start = 0, [FromQuery] int count = 10, [FromQuery] string sortBy = null, [FromQuery] SortDirection sortDirection = SortDirection.Ascending, [FromQuery] bool detailed = false)
        {
            if (start < 0)
            {
                return BadRequest("Start index must be a positive number");
            }
            if (count <= 0)
            {
                return BadRequest("Item count must be greater than zero");
            }

            var query = _db.SteamAssetDescriptions.AsNoTracking();

            filter = Uri.UnescapeDataString(filter?.Trim() ?? string.Empty);
            var filterWords = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var filterWord in filterWords)
            {
                query = query.Where(x =>
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

            if (!string.IsNullOrEmpty(itemType))
            {
                query = query.Where(x => x.ItemType == itemType);
            }
            if (glow)
            {
                query = query.Where(x => x.Tags.Serialised.Contains("glow=") && !x.Tags.Serialised.Contains("glow=false"));
            }
            if (glowsight)
            {
                query = query.Where(x => x.Tags.Serialised.Contains("glowsight=") && !x.Tags.Serialised.Contains("glowsight=false"));
            }
            if (cutout)
            {
                query = query.Where(x => x.Tags.Serialised.Contains("cutout=") && !x.Tags.Serialised.Contains("cutout=false"));
            }

            query = query
                .Include(x => x.App)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .OrderByDescending(x => x.TimeAccepted)
                .ThenByDescending(x => x.TimeCreated);

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
        /// Get all items of the specified type
        /// </summary>
        /// <param name="name">The name of the item type</param>
        /// <param name="marketableItemsOnly">If true, only marketable items are returned. If false, all items are returned</param>
        /// <param name="compareWithItemId">The id of an unrelated item to be included in the list. Helpful when you want to compare an item to the list</param>
        /// <returns>The items of the specified item type</returns>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <response code="200">The list of item details.</response>
        /// <response code="400">If the item type name is missing.</response>
        /// <response code="404">If the item type name doesn't exist (or doesn't contain any marketable items).</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("type/{name}")]
        [ProducesResponseType(typeof(ItemDescriptionWithPriceDTO[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsByType([FromRoute] string name, [FromQuery] bool marketableItemsOnly = false, [FromQuery] ulong? compareWithItemId = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Item type name is missing");
            }

            var assetDescriptions = await _db.SteamAssetDescriptions.AsNoTracking()
                .Where(x =>
                    ((x.ItemType == name) && (!marketableItemsOnly || x.IsMarketable)) ||
                    (compareWithItemId != null && x.ClassId == compareWithItemId)
                )
                .Include(x => x.App)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .OrderBy(x => x.MarketItem.BuyNowPrice)
                .ToListAsync();

            if (assetDescriptions?.Any() != true)
            {
                return NotFound("Item type does not exist, or there are no marketable items found");
            }

            var assetDetails = _mapper.Map<SteamAssetDescription, ItemDescriptionWithPriceDTO>(assetDescriptions, this);
            return Ok(assetDetails);
        }

        /// <summary>
        /// Get all items that belong to the specified collection
        /// </summary>
        /// <param name="name">The name of the item collection</param>
        /// <param name="marketableItemsOnly">If true, only marketable items are returned. If false, all items are returned</param>
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
        public async Task<IActionResult> GetItemsByCollection([FromRoute] string name, [FromQuery] bool marketableItemsOnly = false)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Collection name is missing");
            }

            var assetDescriptions = await _db.SteamAssetDescriptions.AsNoTracking()
                .Where(x => x.ItemCollection == name)
                .Where(x => !marketableItemsOnly || x.IsMarketable)
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
    }
}
