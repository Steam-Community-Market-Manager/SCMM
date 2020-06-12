using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;
using SCMM.Web.Shared.Domain.DTOs.Steam;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Server.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("[controller]")]
    public class SteamStoreItemsController : ControllerBase
    {
        private readonly ILogger<SteamStoreItemsController> _logger;
        private readonly SteamDbContext _db;
        private readonly IMapper _mapper;

        public SteamStoreItemsController(ILogger<SteamStoreItemsController> logger, SteamDbContext db, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public IEnumerable<SteamStoreItemDTO> Get(string filter = null)
        {
            filter = filter?.Trim();
            var items = _db.SteamStoreItems
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Include(x => x.Description.WorkshopFile)
                .Include(x => x.Description.WorkshopFile.Creator)
                .OrderByDescending(x => x.Description.WorkshopFile.Subscriptions)
                .Select(x => _mapper.Map<SteamStoreItemDTO>(x))
                .ToList();

            foreach (var item in items.Where(x => x.Description?.Tags != null))
            {
                if (!item.Description.Tags.ContainsKey("itemclass"))
                {
                    continue;
                }

                // TODO: Do this better, very lazy
                var itemClass = Uri.EscapeDataString(item.Description.Tags["itemclass"]);
                var itemPrice = item.StorePrice;
                var marketRank = _db.SteamApps
                    .Where(x => x.Id == item.App.Id)
                    .Select(app => new
                    {
                        Position = app.MarketItems
                            .Where(x => x.Description.Tags.Serialised.Contains($"itemclass={itemClass}"))
                            .Where(x => x.BuyNowPrice < itemPrice)
                            .Count() + 1,
                        Total = app.MarketItems
                            .Where(x => x.Description.Tags.Serialised.Contains($"itemclass={itemClass}"))
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
