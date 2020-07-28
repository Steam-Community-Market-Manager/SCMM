using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Shared;
using SCMM.Web.Server.Data;
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
                    .Select(p => p.AcceptedOn).Max().UtcDateTime.Date;

                // Store normally updates every thursday or friday around 9pm (UK time)
                do
                {
                    nextStoreUpdateUtc = nextStoreUpdateUtc.AddDays(1);
                } while (nextStoreUpdateUtc.DayOfWeek != DayOfWeek.Thursday);
                
                // If the expected store date is in the past, assume it will be tomorrow
                if ((nextStoreUpdateUtc + TimeSpan.FromHours(3)) <= DateTime.UtcNow)
                {
                    nextStoreUpdateUtc = nextStoreUpdateUtc.AddDays(1);
                }

                nextStoreUpdateUtc = nextStoreUpdateUtc.Add(new TimeSpan(21, 0, 0));
                return new DateTimeOffset(nextStoreUpdateUtc, TimeZoneInfo.Utc.BaseUtcOffset);
            }
        }

        [HttpGet]
        public IEnumerable<StoreItemListDTO> Get()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var latestStoreEnd = db.SteamAssetWorkshopFiles.Select(p => p.AcceptedOn).Max();
                var latestStoreStart = latestStoreEnd.Subtract(TimeSpan.FromHours(24));
                var items = db.SteamStoreItems
                    .Include(x => x.App)
                    .Include(x => x.Description)
                    .Include(x => x.Description.WorkshopFile)
                    .Where(x => x.Description.WorkshopFile.AcceptedOn >= latestStoreStart && x.Description.WorkshopFile.AcceptedOn <= latestStoreEnd)
                    .OrderByDescending(x => x.Description.WorkshopFile.Subscriptions)
                    .Take(100)
                    .ToList();

                var itemDtos = items.ToDictionary(
                    x => x,
                    x => _mapper.Map<StoreItemListDTO>(x, opt => opt.AddRequest(Request))
                );

                // TODO: Do this better, very lazy
                foreach (var pair in itemDtos.Where(x => x.Value.Tags != null))
                {
                    var item = pair.Value;
                    var itemType = String.Empty;
                    if (item.Tags.ContainsKey(SteamConstants.SteamAssetTagItemType))
                    {
                        itemType = Uri.EscapeDataString(
                            item.Tags[SteamConstants.SteamAssetTagItemType]
                        );
                    }
                    else if (item.Tags.ContainsKey(SteamConstants.SteamAssetTagWorkshop))
                    {
                        itemType = Uri.EscapeDataString(
                            item.Tags.FirstOrDefault(x => x.Key.StartsWith(SteamConstants.SteamAssetTagWorkshop)).Value
                        );
                    }
                    if (string.IsNullOrEmpty(itemType))
                    {
                        continue;
                    }

                    var systemCurrency = db.SteamCurrencies.FirstOrDefault(x => x.IsDefault);
                    var itemPrice = item.StorePrice;// systemCurrency.ToLocalValue(item.StorePrice, item.Currency);
                    var marketRank = db.SteamApps
                        .Where(x => x.SteamId == item.SteamAppId)
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
                        item.MarketRankPosition = marketRank.Position;
                        item.MarketRankTotal = marketRank.Total;
                    }
                }

                return itemDtos
                    .Select(x => x.Value)
                    .ToList();
            }
        }
    }
}
