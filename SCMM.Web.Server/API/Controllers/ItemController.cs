using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Steam.API;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.Domain.Item;
using SCMM.Web.Server.Extensions;
using System;
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
        /// Get all items that belong to a given collection
        /// </summary>
        /// <param name="name">The name of the item collection</param>
        /// <returns>The items belonging to the collection</returns>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining the <code>Currency</code> header and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <response code="200">The list of items within the collection.</response>
        /// <response code="400">If the collection name is missing.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("collection/{name}")]
        [ProducesResponseType(typeof(ItemCollectionListItemDTO[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStore([FromRoute] string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return BadRequest("Collection name is missing");
            }

            var assets = await _db.SteamAssetDescriptions.AsNoTracking()
                .Where(x => x.ItemCollection == name)
                .Include(x => x.App)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .ToListAsync();

            var assetDetails = _mapper.Map<SteamAssetDescription, ItemCollectionListItemDTO>(assets, this);
            return Ok(assetDetails);
        }
    }
}
