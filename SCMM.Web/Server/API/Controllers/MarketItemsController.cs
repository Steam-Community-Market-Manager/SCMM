using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain.Models.Steam;
using SCMM.Web.Shared.Domain.DTOs.MarketItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SCMM.Web.Server.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("[controller]")]
    public class MarketItemsController : ControllerBase
    {
        private readonly ILogger<MarketItemsController> _logger;
        private readonly SteamDbContext _db;
        private readonly IMapper _mapper;

        public MarketItemsController(ILogger<MarketItemsController> logger, SteamDbContext db, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public IEnumerable<MarketItemListDTO> Get(
            [FromQuery] string filter = null,
            [FromQuery] string sort = null,
            [FromQuery] bool sortDesc = false,
            [FromQuery] int page = 0, 
            [FromQuery] int pageSize = 25)
        {
            filter = filter?.Trim();
            page = Math.Max(0, page);
            pageSize = Math.Max(0, Math.Min(1000, pageSize));

            var query = _db.SteamMarketItems
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
                    case nameof(MarketItemListDTO.Last24hrSales): orderByExpression = (x) => x.Last24hrSales; break;
                    case nameof(MarketItemListDTO.Last24hrValue): orderByExpression = (x) => x.Last24hrValue; break;
                    case nameof(MarketItemListDTO.Last48hrValue): orderByExpression = (x) => (x.Last24hrValue - x.Last48hrValue); break;
                    case nameof(MarketItemListDTO.Last120hrValue): orderByExpression = (x) => (x.Last24hrValue - x.Last120hrValue); break;
                    case nameof(MarketItemListDTO.BuyNowPrice): orderByExpression = (x) => x.BuyNowPrice; break;
                    case nameof(MarketItemListDTO.BuyAskingPrice): orderByExpression = (x) => x.BuyAskingPrice; break;
                    case nameof(MarketItemListDTO.First24hrValue): orderByExpression = (x) => x.First24hrValue; break;
                    case "Appreciation": orderByExpression = (x) => (x.Last24hrValue - x.First24hrValue); break;
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
                .Select(x => _mapper.Map<MarketItemListDTO>(x))
                .ToList();
        }

        [HttpGet("{id}")]
        public MarketItemDetailDTO Get(Guid id)
        {
            return _db.SteamMarketItems
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Include(x => x.Description.WorkshopFile)
                .Where(x => x.Id == id)
                .Select(x => _mapper.Map<MarketItemDetailDTO>(x))
                .SingleOrDefault();
        }
        
        [HttpGet("dashboard/hotRightNow")]
        public IEnumerable<MarketItemListDTO> GetDashboardHotRightNow()
        {
            return _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Description)
                .OrderByDescending(x => x.Last24hrSales)
                .Take(10)
                .Select(x => _mapper.Map<MarketItemListDTO>(x))
                .ToList();
        }

        [HttpGet("dashboard/allTimeLow")]
        public IEnumerable<MarketItemListDTO> GetDashboardAllTimeLow()
        {
            var lastWeek = DateTime.UtcNow.Subtract(TimeSpan.FromDays(7));
            return _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.Last24hrValue > 50)
                .Where(x => x.FirstSeenOn < lastWeek)
                .OrderBy(x => Math.Abs(x.Last24hrValue - x.AllTimeLowestValue))
                .Take(10)
                .Select(x => _mapper.Map<MarketItemListDTO>(x))
                .ToList();
        }

        [HttpGet("dashboard/stonkingRightNow")]
        public IEnumerable<MarketItemListDTO> GetDashboardStonkingRightNow()
        {
            return _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .OrderByDescending(x => x.Last24hrValue - x.Last48hrValue)
                .Take(10)
                .Select(x => _mapper.Map<MarketItemListDTO>(x))
                .ToList();
        }

        [HttpGet("dashboard/stinkingRightNow")]
        public IEnumerable<MarketItemListDTO> GetDashboardStinkingRightNow()
        {
            return _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .OrderBy(x => x.Last24hrValue - x.Last48hrValue)
                .Take(10)
                .Select(x => _mapper.Map<MarketItemListDTO>(x))
                .ToList();
        }

        [HttpGet("dashboard/mostProfitable")]
        public IEnumerable<MarketItemListDTO> GetDashboardMostProfitable()
        {
            return _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .OrderByDescending(x => x.Last24hrValue - x.First24hrValue)
                .Take(10)
                .Select(x => _mapper.Map<MarketItemListDTO>(x))
                .ToList();
        }

        [HttpGet("dashboard/mostCommon")]
        public IEnumerable<MarketItemListDTO> GetDashboardMostCommon()
        {
            return _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Description)
                .Include(x => x.Description.WorkshopFile)
                .Where(x => x.Description.WorkshopFile.Subscriptions > 0)
                .OrderByDescending(x => x.Description.WorkshopFile.Subscriptions)
                .Take(10)
                .Select(x => _mapper.Map<MarketItemListDTO>(x))
                .ToList();
        }

        [HttpGet("dashboard/mostRare")]
        public IEnumerable<MarketItemListDTO> GetDashboardMostRare()
        {
            return _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Description)
                .Include(x => x.Description.WorkshopFile)
                .Where(x => x.Description.WorkshopFile.Subscriptions > 0)
                .OrderBy(x => x.Description.WorkshopFile.Subscriptions)
                .Take(10)
                .Select(x => _mapper.Map<MarketItemListDTO>(x))
                .ToList();
        }

        [HttpGet("dashboard/biggestCrashes")]
        public IEnumerable<MarketItemListDTO> GetDashboardBiggestCrashes()
        {
            var now = DateTimeOffset.UtcNow;
            return _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.AllTimeHighestValueOn < now)
                .Where(x => x.Last24hrValue < x.AllTimeHighestValue)
                .OrderBy(x => x.Last24hrValue - x.AllTimeHighestValue)
                .Take(10)
                .Select(x => _mapper.Map<MarketItemListDTO>(x))
                .ToList();
        }
    }
}
