using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Shared;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain.Models.Steam;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Shared.Domain.DTOs.MarketItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarketController : ControllerBase
    {
        private readonly ILogger<MarketController> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;

        public MarketController(ILogger<MarketController> logger, IServiceScopeFactory scopeFactory, IMapper mapper)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet]
        public IEnumerable<MarketItemListDTO> Get(
            [FromQuery] string filter = null,
            [FromQuery] string sort = null,
            [FromQuery] bool sortDesc = false,
            [FromQuery] int page = 0,
            [FromQuery] int pageSize = 25)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                filter = Uri.EscapeDataString(filter?.Trim() ?? String.Empty);
                page = Math.Max(0, page);
                pageSize = Math.Max(0, Math.Min(1000, pageSize));

                var query = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Currency)
                    .Include(x => x.Description)
                    .Include(x => x.Description.WorkshopFile)
                    .Where(x => String.IsNullOrEmpty(filter) || x.Description.Name.Contains(filter) || x.Description.Tags.Serialised.Contains(filter));

                Expression<Func<SteamMarketItem, dynamic>> orderByExpression = (x) => x.Description.Name;
                if (!String.IsNullOrEmpty(sort))
                {
                    switch (sort)
                    {
                        case nameof(MarketItemListDTO.Name): orderByExpression = (x) => x.Description.Name; break;
                        case nameof(MarketItemListDTO.MarketAge): orderByExpression = (x) => x.MarketAge; break;
                        case nameof(MarketItemListDTO.Subscriptions): orderByExpression = (x) => x.Description.WorkshopFile.Subscriptions; break;
                        case nameof(MarketItemListDTO.Supply): orderByExpression = (x) => x.Supply; break;
                        case nameof(MarketItemListDTO.Demand): orderByExpression = (x) => x.Demand; break;
                        case nameof(MarketItemListDTO.Last1hrSales): orderByExpression = (x) => x.Last1hrSales; break;
                        case nameof(MarketItemListDTO.Last1hrValue): orderByExpression = (x) => x.Last1hrValue; break;
                        case nameof(MarketItemListDTO.Last24hrValue): orderByExpression = (x) => (x.Last1hrValue - x.Last24hrValue); break;
                        case nameof(MarketItemListDTO.Last120hrValue): orderByExpression = (x) => (x.Last1hrValue - x.Last120hrValue); break;
                        case nameof(MarketItemListDTO.BuyNowPrice): orderByExpression = (x) => x.BuyNowPrice; break;
                        case nameof(MarketItemListDTO.BuyAskingPrice): orderByExpression = (x) => x.BuyAskingPrice; break;
                        case nameof(MarketItemListDTO.First24hrValue): orderByExpression = (x) => x.First24hrValue; break;
                        case "Appreciation": orderByExpression = (x) => (x.Last1hrValue - x.First24hrValue); break;
                    }
                }
                if (sortDesc)
                {
                    query = query.OrderByDescending(orderByExpression);
                }
                else
                {
                    query = query.OrderBy(orderByExpression);
                }

                return query
                    .Skip((page * pageSize))
                    .Take(pageSize)
                    .ToList()
                    .Select(x => _mapper.Map<SteamMarketItem, MarketItemListDTO>(x, this))
                    .ToList();
            }
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public MarketItemDetailDTO Get([FromRoute] Guid id)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var query = db.SteamMarketItems
                    .Include(x => x.Currency)
                    .Include(x => x.Description)
                    .Include(x => x.Description.WorkshopFile)
                    .SingleOrDefault(x => x.Id == id);

                return _mapper.Map<SteamMarketItem, MarketItemDetailDTO>(query, this);
            }
        }

        /// <summary>
        /// Special API for Lovo's bots
        /// </summary>
        /// <param name="idOrName">The item's SCMM GUID or Steam Name</param>
        /// <returns>The market item listing</returns>
        [AllowAnonymous]
        [HttpGet("item/{idOrName}")]
        public MarketItemListDTO Get([FromRoute] string idOrName)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var id = Guid.Empty;
                Guid.TryParse(idOrName, out id);

                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var query = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Currency)
                    .Include(x => x.Description)
                    .Include(x => x.Description.WorkshopFile)
                    .FirstOrDefault(x => x.Id == id || x.Description.Name == idOrName);

                return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
            }
        }

        [AllowAnonymous]
        [HttpGet("dashboard/hotRightNow")]
        public IEnumerable<MarketItemListDTO> GetDashboardHotRightNow()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var query = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Description)
                    .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                    .OrderByDescending(x => x.Last24hrSales)
                    .Take(10);

                return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
            }
        }

        [AllowAnonymous]
        [HttpGet("dashboard/goodTimeToBuy")]
        public IEnumerable<MarketItemListDTO> GetDashboardGoodTimeToBuy()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var now = DateTimeOffset.UtcNow;
                var query = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Currency)
                    .Include(x => x.Description)
                    .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                    .Where(x => x.BuyNowPrice < x.Last48hrValue)
                    .Where(x => x.Last1hrValue < x.AllTimeAverageValue)
                    .Where(x => x.Last1hrValue < x.Last24hrValue)
                    .Where(x => x.Last24hrValue < x.Last48hrValue)
                    .Where(x => x.Last1hrValue > 0 && x.Last48hrValue > 0)
                    .OrderByDescending(x => ((decimal)x.Last48hrValue / x.Last1hrValue) * 100)
                    .Take(10);

                return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
            }
        }

        [AllowAnonymous]
        [HttpGet("dashboard/goodTimeToSell")]
        public IEnumerable<MarketItemListDTO> GetDashboardGoodTimeToSell()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var now = DateTimeOffset.UtcNow;
                var query = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Currency)
                    .Include(x => x.Description)
                    .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                    .Where(x => x.BuyNowPrice > x.Last48hrValue)
                    .Where(x => x.Last1hrValue > x.AllTimeAverageValue)
                    .Where(x => x.Last1hrValue > x.Last24hrValue)
                    .Where(x => x.Last24hrValue > x.Last48hrValue)
                    .Where(x => x.Last1hrValue > 0 && x.Last48hrValue > 0)
                    .OrderByDescending(x => ((decimal)x.Last1hrValue / x.Last48hrValue) * 100)
                    .Take(10);

                return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
            }
        }

        [AllowAnonymous]
        [HttpGet("dashboard/allTimeLow")]
        public IEnumerable<MarketItemListDTO> GetDashboardAllTimeLow()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var yesterday = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(1));
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var query = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Currency)
                    .Include(x => x.Description)
                    .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                    .Where(x => x.Last1hrValue > 50 /* cents */)
                    .Where(x => (x.Last1hrValue - x.AllTimeLowestValue) <= 0)
                    .Where(x => x.SalesHistory.Max(y => y.Timestamp) >= yesterday)
                    .OrderBy(x => Math.Abs(x.Last1hrValue - x.AllTimeLowestValue))
                    .ThenBy(x => x.Last1hrValue - x.Last24hrValue)
                    .Take(10);

                return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
            }
        }

        [AllowAnonymous]
        [HttpGet("dashboard/allTimeHigh")]
        public IEnumerable<MarketItemListDTO> GetDashboardAllTimeHigh()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var yesterday = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(1));
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var query = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Currency)
                    .Include(x => x.Description)
                    .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                    .Where(x => x.Last1hrValue > 50 /* cents */)
                    .Where(x => (x.Last1hrValue - x.AllTimeHighestValue) >= 0)
                    .Where(x => x.SalesHistory.Max(y => y.Timestamp) >= yesterday)
                    .OrderBy(x => Math.Abs(x.Last1hrValue - x.AllTimeHighestValue))
                    .ThenByDescending(x => x.Last1hrValue - x.Last24hrValue)
                    .Take(10);

                return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
            }
        }

        [AllowAnonymous]
        [HttpGet("dashboard/profitableFlips")]
        public IEnumerable<MarketItemListDTO> GetDashboardProfitableFlips()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var now = DateTimeOffset.UtcNow;
                var query = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Currency)
                    .Include(x => x.Description)
                    .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                    .Where(x => x.Last24hrValue > x.Last168hrValue)
                    .Where(x => x.BuyAskingPrice < x.Last24hrValue)
                    .Where(x => x.BuyAskingPrice > x.AllTimeLowestValue)
                    .Where(x => x.BuyNowPrice > x.Last24hrValue)
                    .Where(x => x.BuyNowPrice < x.AllTimeHighestValue)
                    .Where(x => (x.BuyNowPrice - x.BuyAskingPrice - Math.Floor(x.BuyNowPrice * SteamEconomyHelper.SteamFeeMultiplier)) > 0)
                    .OrderByDescending(x => (x.BuyNowPrice - x.BuyAskingPrice - Math.Floor(x.BuyNowPrice * SteamEconomyHelper.SteamFeeMultiplier)))
                    .Take(10);

                return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
            }
        }

        [AllowAnonymous]
        [HttpGet("dashboard/mostRecent")]
        public IEnumerable<MarketItemListDTO> GetDashboardMostRecent()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var query = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Description)
                    .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                    .OrderByDescending(x => x.FirstSeenOn)
                    .Take(10);

                return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
            }
        }

        [AllowAnonymous]
        [HttpGet("dashboard/mostProfitable")]
        public IEnumerable<MarketItemListDTO> GetDashboardMostProfitable()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var query = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Currency)
                    .Include(x => x.Description)
                    .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                    .OrderByDescending(x => x.Last1hrValue - x.First24hrValue)
                    .Take(10);

                return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
            }
        }

        [AllowAnonymous]
        [HttpGet("dashboard/mostSaturated")]
        public IEnumerable<MarketItemListDTO> GetDashboardMostSaturated()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var query = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Description)
                    .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                    .Where(x => x.Supply > 0 && x.Demand > 0) // This doesn't work for some reason?!
                    .OrderByDescending(x => (x.Supply > 0 && x.Demand > 0) ? ((decimal)x.Supply / x.Demand) : 0)
                    .Take(10);

                return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
            }
        }

        [AllowAnonymous]
        [HttpGet("dashboard/mostStarved")]
        public IEnumerable<MarketItemListDTO> GetDashboardMostStarved()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var query = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Description)
                    .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                    .Where(x => x.Supply > 0 && x.Demand > 0) // This doesn't work for some reason?!
                    .OrderBy(x => (x.Supply > 0 && x.Demand > 0) ? ((decimal)x.Supply / x.Demand) : 0)
                    .Take(10);

                return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
            }
        }

        [AllowAnonymous]
        [HttpGet("dashboard/mostCommon")]
        public IEnumerable<MarketItemListDTO> GetDashboardMostCommon()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var query = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Description)
                    .Include(x => x.Description.WorkshopFile)
                    .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                    .Where(x => x.Description.WorkshopFile.Subscriptions > 0)
                    .OrderByDescending(x => x.Description.WorkshopFile.Subscriptions)
                    .Take(10);

                return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
            }
        }

        [AllowAnonymous]
        [HttpGet("dashboard/mostRare")]
        public IEnumerable<MarketItemListDTO> GetDashboardMostRare()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var query = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Description)
                    .Include(x => x.Description.WorkshopFile)
                    .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                    .Where(x => x.Description.WorkshopFile.Subscriptions > 0)
                    .OrderBy(x => x.Description.WorkshopFile.Subscriptions)
                    .Take(10);

                return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
            }
        }

        [AllowAnonymous]
        [HttpGet("dashboard/biggestCrashes")]
        public IEnumerable<MarketItemListDTO> GetDashboardBiggestCrashes()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<SteamDbContext>();
                var now = DateTimeOffset.UtcNow;
                var query = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Currency)
                    .Include(x => x.Description)
                    .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                    .Where(x => x.AllTimeLowestValueOn > x.AllTimeHighestValueOn)
                    .Where(x => x.Last1hrValue < x.AllTimeHighestValue)
                    .OrderBy(x => x.Last1hrValue - x.AllTimeHighestValue)
                    .Take(10);

                return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
            }
        }
    }
}
