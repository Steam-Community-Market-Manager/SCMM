using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Shared;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain.Models.Steam;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Shared.Domain.DTOs.StoreItems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Server.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class StoreController : ControllerBase
    {
        private readonly ILogger<StoreController> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;

        public StoreController(ILogger<StoreController> logger, IServiceScopeFactory scopeFactory, IMapper mapper)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _mapper = mapper;
        }

        [HttpGet("nextUpdateExpectedOn")]
        public DateTimeOffset GetNextUpdateExpectedOn()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var nextStoreUpdateUtc = db.SteamAssetWorkshopFiles
                    .Where(x => x.AcceptedOn != null)
                    .Select(x => x.AcceptedOn.Value)
                    .Max().UtcDateTime;

                // Store normally updates every thursday or friday around 9pm (UK time)
                nextStoreUpdateUtc = (nextStoreUpdateUtc.Date + new TimeSpan(21, 0, 0));
                do
                {
                    nextStoreUpdateUtc = nextStoreUpdateUtc.AddDays(1);
                } while (nextStoreUpdateUtc.DayOfWeek != DayOfWeek.Thursday);

                // If the expected store date is still in the past, assume it is a day late
                // NOTE: Has a tolerance of 3hrs from the expected time
                while ((nextStoreUpdateUtc + TimeSpan.FromHours(3)) <= DateTime.UtcNow)
                {
                    nextStoreUpdateUtc = nextStoreUpdateUtc.AddDays(1);
                }

                return new DateTimeOffset(nextStoreUpdateUtc, TimeZoneInfo.Utc.BaseUtcOffset);
            }
        }

        [HttpGet]
        public IEnumerable<StoreItemListDTO> Get()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var latestStore = db.SteamAssetWorkshopFiles.Select(p => p.AcceptedOn).Max();
                var items = db.SteamStoreItems
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
                    x => _mapper.Map<SteamStoreItem, StoreItemListDTO>(x, Request)
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

                    var systemCurrency = db.SteamCurrencies.FirstOrDefault(x => x.IsDefault);
                    var itemPrice = item.StorePrices.FirstOrDefault(x => x.Key == systemCurrency.Name).Value;
                    if (itemPrice <= 0)
                    {
                        continue;
                    }

                    var marketRank = db.SteamApps
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
}
