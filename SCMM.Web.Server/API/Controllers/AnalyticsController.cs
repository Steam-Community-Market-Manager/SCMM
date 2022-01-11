using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Extensions;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Analytic;
using SCMM.Web.Server.Extensions;
using Syncfusion.Blazor.Data;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    public class AnalyticsController : ControllerBase
    {
        private readonly ILogger<StatisticsController> _logger;
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public AnalyticsController(ILogger<StatisticsController> logger, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        /// <summary>
        /// List items that are cheaper to buy from third party markets rather than the Steam Community Market
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="filter">Optional search filter. Matches against item name</param>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [HttpGet("buyNowDeals")]
        [ProducesResponseType(typeof(PaginatedResult<MarketItemDealAnalyticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBuyNowDeals([FromQuery] string filter = null, [FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => String.IsNullOrEmpty(filter) || x.Description.Name.Contains(filter))
                .Where(x => x.BuyNowFrom != MarketType.SteamCommunityMarket)
                .Where(x => (x.BuyNowPrice + x.BuyNowFee) < x.SellOrderLowestPrice)
                .Where(x => (x.BuyNowPrice + x.BuyNowFee) > 0 && x.SellOrderLowestPrice > 0 && (x.SellOrderLowestPrice - (x.BuyNowPrice + x.BuyNowFee)) > 0)
                .OrderByDescending(x => (x.SellOrderLowestPrice - (x.BuyNowPrice + x.BuyNowFee)) / (decimal)x.SellOrderLowestPrice);

            return Ok(
                await query.PaginateAsync(start, count, x => new MarketItemDealAnalyticDTO()
                {
                    Id = x.Description.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    IconUrl = x.Description.IconUrl,
                    Name = x.Description.Name,
                    BuyFrom = x.BuyNowFrom,
                    BuyPrice = this.Currency().CalculateExchange(x.BuyNowPrice + x.BuyNowFee, x.Currency),
                    BuyUrl = x.Description.GetBuyPrices(x.Currency)?.FirstOrDefault(p => p.MarketType == x.BuyNowFrom)?.Url,
                    ReferenceFrom = MarketType.SteamCommunityMarket,
                    ReferemcePrice = this.Currency().CalculateExchange(x.SellOrderLowestPrice - 1, x.Currency),
                })
            );
        }

        /// <summary>
        /// List items from third party markets that can be instatly flipped on to the Steam Community Market for more than what you paid
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="filter">Optional search filter. Matches against item name</param>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [HttpGet("undervaluedDeals")]
        [ProducesResponseType(typeof(PaginatedResult<MarketItemFlipDealAnalyticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUndervaluedDeals([FromQuery] string filter = null, [FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => String.IsNullOrEmpty(filter) || x.Description.Name.Contains(filter))
                .Where(x => x.BuyNowFrom != MarketType.SteamCommunityMarket)
                .Where(x => (x.BuyNowPrice + x.BuyNowFee) > 0 && x.BuyOrderHighestPrice > 0)
                .Where(x => ((x.BuyOrderHighestPrice - (x.BuyOrderHighestPrice * EconomyExtensions.MarketFeeMultiplier) - (x.BuyNowPrice + x.BuyNowFee)) / (decimal)x.BuyOrderHighestPrice) > 0.25m)
                .OrderByDescending(x => (x.BuyOrderHighestPrice - (x.BuyOrderHighestPrice * EconomyExtensions.MarketFeeMultiplier) - (x.BuyNowPrice + x.BuyNowFee)));

            return Ok(
                await query.PaginateAsync(start, count, x => new MarketItemFlipDealAnalyticDTO()
                {
                    Id = x.Description.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    IconUrl = x.Description.IconUrl,
                    Name = x.Description.Name,
                    BuyFrom = x.BuyNowFrom,
                    BuyPrice = this.Currency().CalculateExchange(x.BuyNowPrice + x.BuyNowFee, x.Currency),
                    BuyUrl = x.Description.GetBuyPrices(x.Currency)?.FirstOrDefault(p => p.MarketType == x.BuyNowFrom)?.Url,
                    SellTo = MarketType.SteamCommunityMarket,
                    SellNowPrice = this.Currency().CalculateExchange(x.BuyOrderHighestPrice, x.Currency),
                    SellNowFee = this.Currency().CalculateExchange(EconomyExtensions.SteamMarketFeeAsInt(x.BuyOrderHighestPrice), x.Currency),
                    SellLaterPrice = this.Currency().CalculateExchange(x.SellOrderLowestPrice - 1, x.Currency),
                    SellLaterFee = this.Currency().CalculateExchange(EconomyExtensions.SteamMarketFeeAsInt(x.SellOrderLowestPrice - 1), x.Currency),
                })
            );
        }
    }
}
