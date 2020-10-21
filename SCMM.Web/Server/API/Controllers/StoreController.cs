using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Shared;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Steam;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Services;
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
        private readonly SteamDbContext _db;
        private readonly SteamService _steam;
        private readonly ImageService _images;
        private readonly IMapper _mapper;

        public StoreController(ILogger<StoreController> logger, SteamDbContext db, SteamService steam, ImageService images, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _steam = steam;
            _images = images;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet("nextUpdateExpectedOn")]
        public DateTimeOffset? GetStoreNextUpdateExpectedOn()
        {
            return _steam.GetStoreNextUpdateExpectedOn();
        }

        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ItemStoreDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStore()
        {
            var latestStore = _db.SteamItemStores
                .GroupBy(x => 1)
                .Select(x => x.Max(y => y.Start))
                .FirstOrDefault();
            var itemStore = _db.SteamItemStores
                .Where(x => x.Start == latestStore)
                .Include(x => x.Items).ThenInclude(x => x.Item)
                .Include(x => x.Items).ThenInclude(x => x.Item.App)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.WorkshopFile)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.WorkshopFile.Creator)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.MarketItem)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.MarketItem.Currency)
                .FirstOrDefault();

            var itemStoreDetail = _mapper.Map<SteamItemStore, ItemStoreDTO>(itemStore, this);
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
        [HttpGet("mosaic")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStoreMosaic()
        {
            var latestStore = _db.SteamItemStores
                .GroupBy(x => 1)
                .Select(x => x.Max(y => y.Start))
                .FirstOrDefault();
            var storeItemDescriptions = _db.SteamItemStores
                .Where(x => x.Start == latestStore)
                .SelectMany(x => x.Items.Select(x => x.Item.Description))
                .OrderBy(x => x.Name)
                .Select(x => x.IconUrl)
                .Take(SteamConstants.SteamStoreItemsMax)
                .ToList();

            if (!storeItemDescriptions.Any())
            {
                return NotFound();
            }

            var mosaic = await _images.GetImageMosaic(
                storeItemDescriptions.Select(x => 
                    new ImageSource()
                    {
                        Url = x
                    }
                ),
                tileSize: 152, 
                columns: 4,
                rows: (int) Math.Ceiling((float) storeItemDescriptions.Count / 4)
            );

            return File(mosaic, "image/png");
        }
    }
}
