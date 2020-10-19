using AutoMapper;
using Microsoft.AspNetCore.Authorization;
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

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoreController : ControllerBase
    {
        private readonly ILogger<StoreController> _logger;
        private readonly SteamDbContext _db;
        private readonly SteamService _steam;
        private readonly IMapper _mapper;

        public StoreController(ILogger<StoreController> logger, SteamDbContext db, SteamService steam, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _steam = steam;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet("nextUpdateExpectedOn")]
        public DateTimeOffset GetNextUpdateExpectedOn()
        {
            return _steam.GetStoreNextUpdateExpectedOn();
        }

        [AllowAnonymous]
        [HttpGet]
        public IEnumerable<StoreItemListDTO> Get()
        {
            var latestStore = _db.SteamAssetWorkshopFiles.Select(p => p.AcceptedOn).Max();
            var items = _db.SteamStoreItems
                .Include(x => x.App)
                .Include(x => x.Description)
                .Include(x => x.Description.WorkshopFile)
                .Include(x => x.Description.WorkshopFile.Creator)
                .Where(x => x.Description.WorkshopFile.AcceptedOn == latestStore)
                .OrderBy(x => x.StoreRankPosition)
                .ThenByDescending(x => x.Description.WorkshopFile.Subscriptions)
                .Take(SteamConstants.SteamStoreItemsMax)
                .ToList();

            var itemDtos = items.ToDictionary(
                x => x,
                x => _mapper.Map<SteamStoreItem, StoreItemListDTO>(x, this)
            );

            // TODO: Do this better, very lazy
            foreach (var pair in itemDtos.Where(x => x.Value.Tags != null))
            {
                var item = pair.Key;
                var itemDto = pair.Value;
                var itemType = Uri.EscapeDataString(itemDto.ItemType ?? String.Empty);
                if (String.IsNullOrEmpty(itemType))
                {
                    continue;
                }

                var systemCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.IsDefault);
                var itemPrice = item.StorePrices.FirstOrDefault(x => x.Key == systemCurrency.Name).Value;
                if (itemPrice <= 0)
                {
                    continue;
                }

                var marketRank = _db.SteamApps
                    .Where(x => x.SteamId == itemDto.SteamAppId)
                    .Select(app => new
                    {
                        Position = app.MarketItems
                            .Where(x => x.Description.Tags.Serialised.Contains(itemType))
                            .Where(x => x.BuyNowPrice < itemPrice)
                            .Count(),
                        Total = app.MarketItems
                            .Where(x => x.Description.Tags.Serialised.Contains(itemType))
                            .Count() + 1,
                    })
                    .SingleOrDefault();

                if (marketRank.Total > 1)
                {
                    itemDto.MarketRankPosition = marketRank.Position;
                    itemDto.MarketRankTotal = marketRank.Total;
                }
            }

            return itemDtos
                .Select(x => x.Value)
                .ToList();
        }
    }
}
