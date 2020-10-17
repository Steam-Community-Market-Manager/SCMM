using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly SteamDbContext _db;
        private readonly IMapper _mapper;

        public MarketController(ILogger<MarketController> logger, SteamDbContext db, IMapper mapper)
        {
            _logger = logger;
            _db = db;
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
            filter = Uri.EscapeDataString(filter?.Trim() ?? String.Empty);
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

        [AllowAnonymous]
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MarketItemDetailDTO), StatusCodes.Status200OK)]
        public IActionResult Get([FromRoute] Guid id)
        {
            var item = _db.SteamMarketItems
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Include(x => x.Description.WorkshopFile)
                .FirstOrDefault(x => x.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            return Ok(
                _mapper.Map<SteamMarketItem, MarketItemDetailDTO>(item, this)
            );
        }

        /// <summary>
        /// Special API for Lovo's bots
        /// </summary>
        /// <param name="idOrName">The item's SCMM GUID or Steam Name</param>
        /// <returns>The market item listing</returns>
        [AllowAnonymous]
        [HttpGet("item/{idOrName}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MarketItemListDTO), StatusCodes.Status200OK)]
        public IActionResult Get([FromRoute] string idOrName)
        {
            var id = Guid.Empty;
            Guid.TryParse(idOrName, out id);

            var item = _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Include(x => x.Description.WorkshopFile)
                .FirstOrDefault(x => (id != Guid.Empty && id == x.Id) || (id == Guid.Empty && idOrName == x.Description.Name));

            if (item == null)
            {
                return NotFound();
            }

            return Ok(
                _mapper.Map<SteamMarketItem, MarketItemListDTO>(item, this)
            );
        }

        [AllowAnonymous]
        [HttpGet("dashboard/salesToday")]
        public int GetSalesToday()
        {
            var from = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(2));
            var to = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var query = _db.SteamMarketItemSale
                .Where(x => x.Timestamp.Date >= from &&  x.Timestamp.Date <= to.Date)
                .GroupBy(x => x.Timestamp.Date)
                .OrderByDescending(x => x.Key.Date)
                .Select(x =>  x.Sum(y => y.Quantity));

            return query.SingleOrDefault();
        }

        [AllowAnonymous]
        [HttpGet("dashboard/salesPerDay")]
        public IDictionary<string, int> GetSalesPerDay([FromQuery] int? maxDays = null)
        {
            var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var query = _db.SteamMarketItemSale
                .Where(x => x.Timestamp.Date <= yesterday.Date)
                .GroupBy(x => x.Timestamp.Date)
                .OrderByDescending(x => x.Key.Date)
                .Select(x => new
                {
                    Date = x.Key,
                    Sales = x.Sum(y => y.Quantity)
                });

            if (maxDays > 0)
            {
                query = query.Take(maxDays.Value);
            }

            var salesPerDay = query.ToList();
            salesPerDay.Reverse(); // newest at bottom
            return salesPerDay.ToDictionary(
                x => x.Date.ToString("dd MMM yyyy"),
                x => x.Sales
            );
        }

        [AllowAnonymous]
        [HttpGet("dashboard/hotRightNow")]
        public IEnumerable<MarketItemListDTO> GetDashboardHotRightNow()
        {
            var query = _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Description)
                .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                .OrderByDescending(x => x.Last24hrSales)
                .Take(10);

            return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
        }

        [AllowAnonymous]
        [HttpGet("dashboard/goodTimeToBuy")]
        public IEnumerable<MarketItemListDTO> GetDashboardGoodTimeToBuy()
        {
            var now = DateTimeOffset.UtcNow;
            var query = _db.SteamMarketItems
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

        [AllowAnonymous]
        [HttpGet("dashboard/goodTimeToSell")]
        public IEnumerable<MarketItemListDTO> GetDashboardGoodTimeToSell()
        {
            var now = DateTimeOffset.UtcNow;
            var query = _db.SteamMarketItems
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

        [AllowAnonymous]
        [HttpGet("dashboard/allTimeLow")]
        public IEnumerable<MarketItemListDTO> GetDashboardAllTimeLow()
        {
            var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var query = _db.SteamMarketItems
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

        [AllowAnonymous]
        [HttpGet("dashboard/allTimeHigh")]
        public IEnumerable<MarketItemListDTO> GetDashboardAllTimeHigh()
        {
            var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var query = _db.SteamMarketItems
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

        [AllowAnonymous]
        [HttpGet("dashboard/profitableFlips")]
        public IEnumerable<MarketItemListDTO> GetDashboardProfitableFlips()
        {
            var now = DateTimeOffset.UtcNow;
            var query = _db.SteamMarketItems
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

        [AllowAnonymous]
        [HttpGet("dashboard/mostRecent")]
        public IEnumerable<MarketItemListDTO> GetDashboardMostRecent()
        {
            var query = _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Description)
                .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                .OrderByDescending(x => x.FirstSeenOn)
                .Take(10);

            return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
        }

        [AllowAnonymous]
        [HttpGet("dashboard/mostProfitable")]
        public IEnumerable<MarketItemListDTO> GetDashboardMostProfitable()
        {
            var query = _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                .OrderByDescending(x => x.Last1hrValue - x.First24hrValue)
                .Take(10);

            return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
        }

        [AllowAnonymous]
        [HttpGet("dashboard/mostSaturated")]
        public IEnumerable<MarketItemListDTO> GetDashboardMostSaturated()
        {
            var query = _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Description)
                .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                .Where(x => x.Supply > 0 && x.Demand > 0) // This doesn't work for some reason?!
                .OrderByDescending(x => (x.Supply > 0 && x.Demand > 0) ? ((decimal)x.Supply / x.Demand) : 0)
                .Take(10);

            return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
        }

        [AllowAnonymous]
        [HttpGet("dashboard/mostStarved")]
        public IEnumerable<MarketItemListDTO> GetDashboardMostStarved()
        {
            var query = _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Description)
                .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                .Where(x => x.Supply > 0 && x.Demand > 0) // This doesn't work for some reason?!
                .OrderBy(x => (x.Supply > 0 && x.Demand > 0) ? ((decimal)x.Supply / x.Demand) : 0)
                .Take(10);

            return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
        }

        [AllowAnonymous]
        [HttpGet("dashboard/mostCommon")]
        public IEnumerable<MarketItemListDTO> GetDashboardMostCommon()
        {
            var query = _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Description)
                .Include(x => x.Description.WorkshopFile)
                .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                .Where(x => x.Description.WorkshopFile.Subscriptions > 0)
                .OrderByDescending(x => x.Description.WorkshopFile.Subscriptions)
                .Take(10);

            return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
        }

        [AllowAnonymous]
        [HttpGet("dashboard/mostRare")]
        public IEnumerable<MarketItemListDTO> GetDashboardMostRare()
        {
            var query = _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Description)
                .Include(x => x.Description.WorkshopFile)
                .Where(x => x.Description.WorkshopFile != null) // Exclude "free" items
                .Where(x => x.Description.WorkshopFile.Subscriptions > 0)
                .OrderBy(x => x.Description.WorkshopFile.Subscriptions)
                .Take(10);

            return _mapper.Map<SteamMarketItem, MarketItemListDTO>(query, this);
        }

        [AllowAnonymous]
        [HttpGet("dashboard/biggestCrashes")]
        public IEnumerable<MarketItemListDTO> GetDashboardBiggestCrashes()
        {
            var now = DateTimeOffset.UtcNow;
            var query = _db.SteamMarketItems
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
