using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Steam.API;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Server.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        /// Get all marketable items of the specific item type
        /// </summary>
        /// <param name="itemType">The name of the item type</param>
        /// <returns>The items of the specified item type</returns>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <response code="200">The list of item details.</response>
        /// <response code="400">If the item type name is missing.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("marketPriceRank/{itemType}")]
        [ProducesResponseType(typeof(ItemDetailsDTO[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsByType([FromRoute] string itemType, string storeItemId = null)
        {
            if (String.IsNullOrEmpty(itemType))
            {
                return BadRequest("Item type name is missing");
            }

            var assetDescriptions = await _db.SteamAssetDescriptions.AsNoTracking()
                .Where(x => 
                    (x.ItemType == itemType && x.IsMarketable && x.MarketItem != null) ||
                    (x.StoreItem != null && x.StoreItem.SteamId == storeItemId)
                )
                .Include(x => x.App)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .OrderBy(x => x.MarketItem.BuyNowPrice)
                .ToListAsync();

            var assetDetails = _mapper.Map<SteamAssetDescription, ItemDetailsDTO>(assetDescriptions, this);
            return Ok(assetDetails);
        }

        /// <summary>
        /// Get all items that belong to the specified collection
        /// </summary>
        /// <param name="collectionName">The name of the item collection</param>
        /// <returns>The items belonging to the collection</returns>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <response code="200">The item collection details.</response>
        /// <response code="400">If the collection name is missing.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("collection/{collectionName}")]
        [ProducesResponseType(typeof(ItemCollectionDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsByCollection([FromRoute] string collectionName)
        {
            if (String.IsNullOrEmpty(collectionName))
            {
                return BadRequest("Collection name is missing");
            }

            var assetDescriptions = await _db.SteamAssetDescriptions.AsNoTracking()
                .Where(x => x.ItemCollection == collectionName)
                .Include(x => x.App)
                .Include(x => x.Creator)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .OrderByDescending(x => x.TimeCreated)
                .ToListAsync();

            var assetDetails = _mapper.Map<List<SteamAssetDescription>, ItemCollectionDTO>(assetDescriptions, this);
            return Ok(assetDetails);
        }
    }
}
