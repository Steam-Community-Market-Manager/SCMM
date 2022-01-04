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
        /// List undervalued items from third party markets that can be sold for profit by flipping them on the Steam Community Market
        /// </summary>
        /// <remarks>
        /// This API requires authentication and the user must belong to the <code>VIP</code> role.
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(Roles = $"{Roles.Administrator},{Roles.VIP}")]
        [HttpGet("undervaluedItems")]
        [ProducesResponseType(typeof(PaginatedResult<MarketItemFlipSaleAnalyticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUndervaluedItems([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.BuyNowFrom != PriceType.SteamCommunityMarket)
                .Where(x => x.BuyNowPrice > 0 && x.BuyOrderHighestPrice > 0)
                .Where(x => ((x.BuyOrderHighestPrice - (x.BuyOrderHighestPrice * EconomyExtensions.MarketFeeMultiplier) - x.BuyNowPrice) / (decimal)x.BuyOrderHighestPrice) > 0.25m)
                .OrderByDescending(x => (x.BuyOrderHighestPrice - (x.BuyOrderHighestPrice * EconomyExtensions.MarketFeeMultiplier) - x.BuyNowPrice));

            return Ok(
                await query.PaginateAsync(start, count, x => new MarketItemFlipSaleAnalyticDTO()
                {
                    Id = x.Description.ClassId,
                    AppId = ulong.Parse(x.App.SteamId),
                    IconUrl = x.Description.IconUrl,
                    Name = x.Description.Name,
                    BuyFrom = x.BuyNowFrom,
                    BuyPrice = this.Currency().CalculateExchange(x.BuyNowPrice, x.Currency),
                    BuyUrl = x.Description.GetPrices(x.Currency)?.FirstOrDefault(p => p.Type == x.BuyNowFrom)?.Url,
                    SellTo = PriceType.SteamCommunityMarket,
                    SellNowPrice = this.Currency().CalculateExchange(x.BuyOrderHighestPrice, x.Currency),
                    SellNowTax = this.Currency().CalculateExchange(EconomyExtensions.SteamMarketFeeAsInt(x.BuyOrderHighestPrice), x.Currency),
                    SellLaterPrice = this.Currency().CalculateExchange(x.SellOrderLowestPrice - 1, x.Currency),
                    SellLaterTax = this.Currency().CalculateExchange(EconomyExtensions.SteamMarketFeeAsInt(x.SellOrderLowestPrice - 1), x.Currency),
                })
            );
        }
    }
}
