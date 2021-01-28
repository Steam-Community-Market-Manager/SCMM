using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Shared;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Steam;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Shared;
using SCMM.Web.Shared.Data.Models.UI;
using SCMM.Web.Shared.Data.Models.UI.MarketStatistics;
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
        private readonly ScmmDbContext _db;
        private readonly IMapper _mapper;

        public MarketController(ILogger<MarketController> logger, ScmmDbContext db, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet]
        public MarketItemListPaginatedDTO Get(
            [FromQuery] string filter = null,
            [FromQuery] string sort = null,
            [FromQuery] bool sortDesc = false,
            [FromQuery] int page = 0,
            [FromQuery] int pageSize = 30)
        {
            filter = Uri.UnescapeDataString(filter?.Trim() ?? String.Empty);
            page = Math.Max(0, page);
            pageSize = Math.Max(0, pageSize);

            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Include(x => x.Description.WorkshopFile)
                .Include(x => x.Description.StoreItem)
                .Where(x => String.IsNullOrEmpty(filter) || x.Description.Name.Contains(filter) || x.Description.Tags.Serialised.Contains(filter));

            Expression<Func<SteamMarketItem, dynamic>> orderByExpression = (x) => x.Description.Name;
            if (!String.IsNullOrEmpty(sort))
            {
                switch (sort)
                {
                    case nameof(MarketItemListDTO.Name): orderByExpression = (x) => x.Description.Name; break;
                    case nameof(MarketItemListDTO.Supply): orderByExpression = (x) => x.Supply; break;
                    case nameof(MarketItemListDTO.Demand): orderByExpression = (x) => x.Demand; break;
                    case nameof(MarketItemListDTO.StorePrice): orderByExpression = (x) => (x.Description.StoreItem == null ? x.Description.StoreItem.Price : 0); break;
                    case nameof(MarketItemListDTO.BuyAskingPrice): orderByExpression = (x) => x.BuyAskingPrice; break;
                    case nameof(MarketItemListDTO.BuyNowPrice): orderByExpression = (x) => x.BuyNowPrice; break;
                    case nameof(MarketItemListDTO.ResellPrice): orderByExpression = (x) => x.ResellPrice; break;
                    case nameof(MarketItemListDTO.ResellTax): orderByExpression = (x) => x.ResellTax; break;
                    case nameof(MarketItemListDTO.ResellProfit): orderByExpression = (x) => x.ResellProfit; break;
                    case nameof(MarketItemListDTO.Last1hrSales): orderByExpression = (x) => x.Last1hrSales; break;
                    case nameof(MarketItemListDTO.Last1hrValue): orderByExpression = (x) => x.Last1hrValue; break;
                    case nameof(MarketItemListDTO.Last24hrSales): orderByExpression = (x) => x.Last24hrSales; break;
                    case nameof(MarketItemListDTO.Last24hrValue): orderByExpression = (x) => x.Last24hrValue; break;
                    case nameof(MarketItemListDTO.Last48hrSales): orderByExpression = (x) => x.Last48hrSales; break;
                    case nameof(MarketItemListDTO.Last48hrValue): orderByExpression = (x) => x.Last48hrValue; break;
                    case nameof(MarketItemListDTO.MovementLast48hrValue): orderByExpression = (x) => x.Last1hrValue - x.Last48hrValue; break;
                    case nameof(MarketItemListDTO.MovementLast120hrValue): orderByExpression = (x) => x.Last1hrValue - x.Last120hrValue; break;
                    case nameof(MarketItemListDTO.MovementLast336hrValue): orderByExpression = (x) => x.Last1hrValue - x.Last336hrValue; break;
                    case nameof(MarketItemListDTO.MarketAge): orderByExpression = (x) => x.FirstSeenOn; break;
                    case nameof(MarketItemListDTO.Subscriptions): orderByExpression = (x) => (x.Description.WorkshopFile != null ? x.Description.WorkshopFile.Subscriptions : 0); break;
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

            var total = query.Count();
            var items = query
                .Skip((page * pageSize))
                .Take(pageSize)
                .ToList()
                .Select(x => _mapper.Map<SteamMarketItem, MarketItemListDTO>(x, this))
                .ToList();

            if (User.Identity.IsAuthenticated)
            {
                var profileId = User.Id();
                var assetDescriptionIds = items.Select(x => x.SteamDescriptionId).ToList();
                var profileMarketItems = _db.SteamProfileMarketItems
                    .AsNoTracking()
                    .Where(x => x.ProfileId == profileId)
                    .Where(x => assetDescriptionIds.Contains(x.Description.SteamId))
                    .Select(x => new
                    {
                        SteamDescriptionId = x.Description.SteamId,
                        Flags = x.Flags
                    })
                    .ToList();
                foreach (var profileMarketItem in profileMarketItems)
                {
                    var item = items.FirstOrDefault(x => x.SteamDescriptionId == profileMarketItem.SteamDescriptionId);
                    if (item != null)
                    {
                        item.ProfileFlags = profileMarketItem.Flags;
                    }
                }
            }

            return new MarketItemListPaginatedDTO()
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.ToArray()
            };
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MarketItemDetailDTO), StatusCodes.Status200OK)]
        public IActionResult Get([FromRoute] Guid id)
        {
            var item = _db.SteamMarketItems
                .AsNoTracking()
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
                .AsNoTracking()
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
                .AsNoTracking()
                .Where(x => x.Timestamp.Date >= from && x.Timestamp.Date <= to.Date)
                .GroupBy(x => x.Timestamp.Date)
                .OrderByDescending(x => x.Key.Date)
                .Select(x => x.Sum(y => y.Quantity));

            return query.SingleOrDefault();
        }

        [AllowAnonymous]
        [HttpGet("dashboard/salesPerDay")]
        public IDictionary<string, DashboardSalesDataDTO> GetSalesPerDay([FromQuery] int? maxDays = null)
        {
            var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var query = _db.SteamMarketItemSale
                .AsNoTracking()
                .Where(x => x.Timestamp.Date <= yesterday.Date)
                .GroupBy(x => x.Timestamp.Date)
                .OrderByDescending(x => x.Key.Date)
                .Select(x => new
                {
                    Date = x.Key,
                    Sales = x.Sum(y => y.Quantity),
                    Revenue = x.Sum(y => y.Quantity * y.Price)
                });

            if (maxDays > 0)
            {
                query = query.Take(maxDays.Value);
            }

            var salesPerDay = query.ToList();
            salesPerDay.Reverse(); // newest at bottom
            return salesPerDay.ToDictionary(
                x => x.Date.ToString("dd MMM yyyy"),
                x => new DashboardSalesDataDTO 
                {
                    Sales = x.Sales,
                    Revenue = x.Revenue
                }
            );
        }

        [AllowAnonymous]
        [HttpGet("dashboard/hotRightNow")]
        public PaginatedResult<DashboardAssetSalesDTO> GetDashboardHotRightNow([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Description)
                .Where(x => x.Last24hrSales > 0)
                .OrderByDescending(x => x.Last24hrSales)
                .Select(x => new DashboardAssetSalesDTO()
                {
                    SteamId = x.Description.SteamId,
                    SteamAppId = x.App.SteamId,
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Last24hrSales = x.Last24hrSales
                });

            return query.Paginate(start, count);
        }

        [AllowAnonymous]
        [HttpGet("dashboard/mostRecent")]
        public PaginatedResult<DashboardAssetAgeDTO> GetDashboardMostRecent([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Description)
                .Where(x => x.FirstSeenOn != null)
                .OrderByDescending(x => x.FirstSeenOn);

            return query.Paginate(start, count, x => new DashboardAssetAgeDTO()
            {
                SteamId = x.Description.SteamId,
                SteamAppId = x.App.SteamId,
                Name = x.Description.Name,
                BackgroundColour = x.Description.BackgroundColour,
                ForegroundColour = x.Description.ForegroundColour,
                IconUrl = x.Description.IconUrl,
                MarketAge = x.MarketAge.ToMarketAgeString()
            });
        }

        [AllowAnonymous]
        [HttpGet("dashboard/allTimeHigh")]
        public PaginatedResult<DashboardAssetMarketValueDTO> GetDashboardAllTimeHigh([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => (x.Last1hrValue - x.AllTimeHighestValue) >= 0)
                .Where(x => x.SalesHistory.Max(y => y.Timestamp) >= yesterday)
                .OrderBy(x => Math.Abs(x.Last1hrValue - x.AllTimeHighestValue))
                .ThenByDescending(x => x.Last1hrValue - x.Last24hrValue);

            return query.Paginate(start, count, x => new DashboardAssetMarketValueDTO()
            {
                SteamId = x.Description.SteamId,
                SteamAppId = x.App.SteamId,
                Name = x.Description.Name,
                BackgroundColour = x.Description.BackgroundColour,
                ForegroundColour = x.Description.ForegroundColour,
                IconUrl = x.Description.IconUrl,
                Currency = this.Currency(),
                Last1hrValue = this.Currency().CalculateExchange(x.Last1hrValue, x.Currency)
            });
        }

        [AllowAnonymous]
        [HttpGet("dashboard/allTimeLow")]
        public PaginatedResult<DashboardAssetMarketValueDTO> GetDashboardAllTimeLow([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => (x.Last1hrValue - x.AllTimeLowestValue) <= 0)
                .Where(x => x.SalesHistory.Max(y => y.Timestamp) >= yesterday)
                .OrderBy(x => Math.Abs(x.Last1hrValue - x.AllTimeLowestValue))
                .ThenBy(x => x.Last1hrValue - x.Last24hrValue);

            return query.Paginate(start, count, x => new DashboardAssetMarketValueDTO()
            {
                SteamId = x.Description.SteamId,
                SteamAppId = x.App.SteamId,
                Name = x.Description.Name,
                BackgroundColour = x.Description.BackgroundColour,
                ForegroundColour = x.Description.ForegroundColour,
                IconUrl = x.Description.IconUrl,
                Currency = this.Currency(),
                Last1hrValue = this.Currency().CalculateExchange(x.Last1hrValue, x.Currency)
            });
        }


        [AllowAnonymous]
        [HttpGet("dashboard/profitableFlips")]
        public PaginatedResult<DashboardAssetBuyOrderValueDTO> GetDashboardProfitableFlips([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var now = DateTimeOffset.UtcNow;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.Last24hrValue > x.Last168hrValue)
                .Where(x => x.BuyAskingPrice < x.Last24hrValue)
                .Where(x => x.BuyAskingPrice > x.AllTimeLowestValue)
                .Where(x => x.BuyNowPrice > x.Last24hrValue)
                .Where(x => x.BuyNowPrice < x.AllTimeHighestValue)
                .Where(x => (x.BuyNowPrice - x.BuyAskingPrice - Math.Floor(x.BuyNowPrice * SteamEconomyHelper.SteamFeeMultiplier)) > 0)
                .OrderByDescending(x => (x.BuyNowPrice - x.BuyAskingPrice - Math.Floor(x.BuyNowPrice * SteamEconomyHelper.SteamFeeMultiplier)));

            return query.Paginate(start, count, x => new DashboardAssetBuyOrderValueDTO()
            {
                SteamId = x.Description.SteamId,
                SteamAppId = x.App.SteamId,
                Name = x.Description.Name,
                BackgroundColour = x.Description.BackgroundColour,
                ForegroundColour = x.Description.ForegroundColour,
                IconUrl = x.Description.IconUrl,
                Currency = this.Currency(),
                BuyNowPrice = this.Currency().CalculateExchange(x.BuyNowPrice, x.Currency),
                BuyAskingPrice = this.Currency().CalculateExchange(x.BuyAskingPrice, x.Currency)
            });
        }

        [AllowAnonymous]
        [HttpGet("dashboard/mostProfitable")]
        public PaginatedResult<DashboardAssetMarketValueDTO> GetDashboardMostProfitable([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.Last1hrValue > 0)
                .OrderByDescending(x => x.Last1hrValue);

            return query.Paginate(start, count, x => new DashboardAssetMarketValueDTO()
            {
                SteamId = x.Description.SteamId,
                SteamAppId = x.App.SteamId,
                Name = x.Description.Name,
                BackgroundColour = x.Description.BackgroundColour,
                ForegroundColour = x.Description.ForegroundColour,
                IconUrl = x.Description.IconUrl,
                Currency = this.Currency(),
                Last1hrValue = this.Currency().CalculateExchange(x.Last1hrValue, x.Currency)
            });
        }

        [AllowAnonymous]
        [HttpGet("dashboard/mostPopular")]
        public PaginatedResult<DashboardAssetSubscriptionsDTO> GetDashboardMostPopular([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamAssetDescriptions
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.WorkshopFile)
                .Where(x => x.WorkshopFile != null && x.WorkshopFile.Subscriptions > 0)
                .OrderByDescending(x => x.WorkshopFile.Subscriptions)
                .Select(x => new DashboardAssetSubscriptionsDTO()
                {
                    SteamId = x.SteamId,
                    SteamAppId = x.App.SteamId,
                    Name = x.Name,
                    BackgroundColour = x.BackgroundColour,
                    ForegroundColour = x.ForegroundColour,
                    IconUrl = x.IconUrl,
                    Subscriptions = x.WorkshopFile.Subscriptions
                });

            return query.Paginate(start, count);
        }

        [AllowAnonymous]
        [HttpGet("dashboard/mostSaturated")]
        public PaginatedResult<DashboardAssetSupplyDemandDTO> GetDashboardMostSaturated([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Description)
                .Where(x => x.Supply > 0)
                .OrderByDescending(x => x.Supply)
                .Select(x => new DashboardAssetSupplyDemandDTO()
                {
                    SteamId = x.Description.SteamId,
                    SteamAppId = x.App.SteamId,
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Supply = x.Supply,
                    Demand = (int) x.Last24hrSales
                });

            return query.Paginate(start, count);
        }

        [AllowAnonymous]
        [HttpGet("dashboard/acceptedSkinAuthors")]
        public PaginatedResult<DashboardProfileWorkshopValueDTO> GetDashboardAcceptedSkinAuthors([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamProfiles
                .AsNoTracking()
                .Where(x => x.WorkshopFiles.Count > 0)
                .OrderByDescending(x => x.WorkshopFiles.Count)
                .Select(x => new DashboardProfileWorkshopValueDTO()
                {
                    SteamId = x.SteamId,
                    Name = x.Name,
                    AvatarUrl = x.AvatarLargeUrl,
                    Items = x.WorkshopFiles.Count,
                });

            return query.Paginate(start, count);
        }
    }
}
