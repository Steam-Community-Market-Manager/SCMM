using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Shared;
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
        public IEnumerable<SteamStoreItemDTO> Get()
        {
            var currentWeek = _db.SteamAssetWorkshopFiles.Select(p => p.AcceptedOn).Max();
            var items = _db.SteamStoreItems
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Include(x => x.Description.WorkshopFile)
                .Include(x => x.Description.WorkshopFile.Creator)
                .Where(x => x.Description.WorkshopFile.AcceptedOn == currentWeek)
                .OrderByDescending(x => x.Description.WorkshopFile.Subscriptions)
                .Select(x => _mapper.Map<SteamStoreItemDTO>(x))
                .ToList();

            // TODO: Do this better, very lazy
            foreach (var item in items.Where(x => x.Description?.Tags != null))
            {
                var type = Uri.EscapeDataString(item.Description.Tags["itemclass"]);
                if (String.IsNullOrEmpty(type))
                {
                    type = Uri.EscapeDataString(
                        item.Description.Tags.FirstOrDefault(x => x.Key.StartsWith(SteamConstants.SteamAssetTagWorkshop)).Value
                    );
                }

                var itemPrice = item.StorePrice;
                var marketRank = _db.SteamApps
                    .Where(x => x.Id == item.App.Id)
                    .Select(app => new
                    {
                        Position = app.MarketItems
                            .Where(x => x.Description.Tags.Serialised.Contains(type))
                            .Where(x => x.BuyNowPrice < itemPrice)
                            .Count(),
                        Total = app.MarketItems
                            .Where(x => x.Description.Tags.Serialised.Contains(type))
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
