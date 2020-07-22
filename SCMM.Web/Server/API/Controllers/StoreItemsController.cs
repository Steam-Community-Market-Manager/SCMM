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
    [Route("[controller]")]
    public class StoreItemsController : ControllerBase
    {
        private readonly ILogger<StoreItemsController> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;

        public StoreItemsController(ILogger<StoreItemsController> logger, IServiceScopeFactory scopeFactory, IMapper mapper)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _mapper = mapper;
        }

        [HttpGet]
        public IEnumerable<StoreItemListDTO> Get()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var currency = db.SteamCurrencies.FirstOrDefault(x => x.IsDefault);
                var latestStoreEnd = db.SteamAssetWorkshopFiles.Select(p => p.AcceptedOn).Max();
                var latestStoreStart = latestStoreEnd.Subtract(TimeSpan.FromDays(2));
                var items = db.SteamStoreItems
                    .Include(x => x.App)
                    .Include(x => x.Description)
                    .Include(x => x.Description.WorkshopFile)
                    .Where(x => x.Description.WorkshopFile.AcceptedOn >= latestStoreStart && x.Description.WorkshopFile.AcceptedOn <= latestStoreEnd)
                    .OrderByDescending(x => x.Description.WorkshopFile.Subscriptions)
                    .Take(100)
                    .Select(x => _mapper.Map<StoreItemListDTO>(x))
                    .ToList();

                // TODO: Do this better, very lazy
                foreach (var item in items.Where(x => x.Tags != null))
                {
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

                    var itemPrice = item.StorePrices.FirstOrDefault(x => x.Key == currency?.Name).Value;
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

                return items;
            }
        }
    }
}
