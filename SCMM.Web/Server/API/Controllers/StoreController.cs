using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Steam;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Services;
using SCMM.Web.Server.Services.Queries;
using SCMM.Web.Shared.Domain.DTOs.StoreItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoreController : ControllerBase
    {
        private readonly ILogger<StoreController> _logger;
        private readonly ScmmDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        private readonly SteamService _steam;

        public StoreController(ILogger<StoreController> logger, ScmmDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper, SteamService steam)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
            _steam = steam;
        }

        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(IEnumerable<ItemStoreListDTO>), StatusCodes.Status200OK)]
        public IActionResult GetStores()
        {
            var appId = this.App();
            var itemStores = _db.SteamItemStores
                .AsNoTracking()
                .Where(x => x.App.SteamId == appId)
                .OrderBy(x => x.Start)
                .ToList();

            var itemStoresList = itemStores.Select(x => _mapper.Map<SteamItemStore, ItemStoreListDTO>(x, this));
            if (!itemStoresList.Any())
            {
                return NotFound();
            }

            return Ok(itemStoresList);
        }

        [AllowAnonymous]
        [HttpGet("current")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ItemStoreDetailedDTO), StatusCodes.Status200OK)]
        public IActionResult GetCurrentStore()
        {
            var appId = this.App();
            var latestItemStoreId = _db.SteamItemStores
                .AsNoTracking()
                .Where(x => x.App.SteamId == appId)
                .Where(x => x.End == null)
                .OrderByDescending(x => x.Start)
                .Select(x => x.Id)
                .FirstOrDefault();

            if (latestItemStoreId == Guid.Empty)
            {
                return NotFound();
            }

            return GetStore(latestItemStoreId);
        }

        [AllowAnonymous]
        [HttpGet("{storeId}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ItemStoreDetailedDTO), StatusCodes.Status200OK)]
        public IActionResult GetStore([FromRoute] Guid storeId)
        {
            var itemStore = _db.SteamItemStores
                .AsNoTracking()
                .Where(x => x.Id == storeId)
                .Include(x => x.Items).ThenInclude(x => x.Item)
                .Include(x => x.Items).ThenInclude(x => x.Item.App)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.WorkshopFile)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.WorkshopFile.Creator)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.MarketItem)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.MarketItem.Currency)
                .FirstOrDefault();

            var itemStoreDetail = _mapper.Map<SteamItemStore, ItemStoreDetailedDTO>(itemStore, this);
            if (itemStoreDetail == null)
            {
                return NotFound();
            }

            // TODO: Do this better, very lazy
            var itemStoreTaggedItems = itemStoreDetail.Items.Where(x => x.Tags != null);
            foreach (var item in itemStoreTaggedItems)
            {
                var itemType = Uri.EscapeDataString(item.ItemType ?? String.Empty);
                if (String.IsNullOrEmpty(itemType))
                {
                    continue;
                }

                var itemPrice = itemStore.Items.Select(x => x.Item).FirstOrDefault(x => x.SteamId == item.SteamId)?.Price;
                if (itemPrice == null || itemPrice <= 0)
                {
                    continue;
                }

                var marketRank = _db.SteamApps
                    .Where(x => x.SteamId == item.SteamAppId)
                    .Select(app => new
                    {
                        Position = app.MarketItems
                            .Where(x => x.Description.Tags.Serialised.Contains(itemType))
                            .Where(x => x.BuyNowPrice < itemPrice.Value)
                            .Count(),
                        Total = app.MarketItems
                            .Where(x => x.Description.Tags.Serialised.Contains(itemType))
                            .Count() + 1,
                    })
                    .SingleOrDefault();

                if (marketRank.Total > 1)
                {
                    item.MarketRankPosition = marketRank.Position;
                    item.MarketRankTotal = marketRank.Total;
                }
            }

            return Ok(itemStoreDetail);
        }

        [AllowAnonymous]
        [HttpGet("nextUpdateTime")]
        public async Task<DateTimeOffset?> GetStoreNextUpdateTime()
        {
            var nextUpdateTime = await _queryProcessor.ProcessAsync(new GetStoreNextUpdateTimeRequest());
            return nextUpdateTime?.Timestamp;
        }
    }
}
