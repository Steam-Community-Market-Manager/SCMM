using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Abstractions.Statistics;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Models.Statistics;
using SCMM.Shared.Data.Store.Extensions;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Analytic;
using SCMM.Web.Data.Models.UI.Profile;
using SCMM.Web.Data.Models.UI.Statistic;
using SCMM.Web.Server.Extensions;
using Syncfusion.Blazor.Data;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/stats")]
    public class StatisticsController : ControllerBase
    {
        private readonly ILogger<StatisticsController> _logger;
        private readonly SteamDbContext _db;
        private readonly IStatisticsService _statisticsService;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public StatisticsController(ILogger<StatisticsController> logger, SteamDbContext db, IStatisticsService statisticsService, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _statisticsService = statisticsService;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        /// <summary>
        /// Get marketplace totals
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// Response is cached for 1hr.
        /// </remarks>
        /// <response code="200">Marketplace totals for the current app.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("market/totals")]
        [ProducesResponseType(typeof(MarketTotalsStatisticDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OutputCache(PolicyName = CachePolicy.Expire1h, Tags = [CacheTag.Statistics])]
        public async Task<IActionResult> GetMarketTotals()
        {
            var appGuid = this.App().Guid;
            var totals = await _db.SteamApps.AsNoTracking()
                .Where(x => x.Id == appGuid)
                .Select(x => new
                {
                    TotalListings = x.MarketItems.Sum(i => i.SellOrderCount),
                    TotalListingsMarketValue = x.MarketItems.Sum(i => i.SellOrderCount * i.SellOrderLowestPrice),
                    TotalVolumeLast24hrs = x.MarketItems.Sum(i => i.Last24hrSales),
                    TotalVolumeMarketValueLast24hrs = x.MarketItems.Sum(i => i.Last24hrSales * i.Last24hrValue),
                    OldestItemSalesUpdate = x.MarketItems.Min(i => i.LastCheckedSalesOn)
                })
                .FirstOrDefaultAsync();

            var last24hrSalesDataIsComplete = (totals?.OldestItemSalesUpdate != null && (DateTimeOffset.Now - totals.OldestItemSalesUpdate.Value).Duration() < TimeSpan.FromDays(1));
            return Ok(new MarketTotalsStatisticDTO()
            {
                Listings = (int)(totals?.TotalListings ?? 0),
                ListingsMarketValue = this.Currency().CalculateExchange((long)(totals?.TotalListingsMarketValue ?? 0L)),
                VolumeLast24hrs = last24hrSalesDataIsComplete ? (int)totals.TotalVolumeLast24hrs : null,
                VolumeMarketValueLast24hrs = last24hrSalesDataIsComplete ? this.Currency().CalculateExchange((long)totals.TotalVolumeMarketValueLast24hrs) : null
            });
        }

        /// <summary>
        /// Get marketplace index fund chart data, grouped by day (UTC)
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="maxDays">The maximum number of days worth of market history to return. Use <code>-1</code> for all history</param>
        /// <response code="200">List of market index fund values grouped/keyed per day by UTC date.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("market/indexFund")]
        [ProducesResponseType(typeof(IEnumerable<MarketIndexFundChartPointDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMarketIndexFund(int maxDays = 30)
        {
            var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var maxDaysCutoff = (maxDays >= 1 ? DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(maxDays)) : (DateTimeOffset?)null);

            var appId = this.App().Id;
            var indexFund = await _statisticsService.GetDictionaryAsync<DateTime, MarketIndexFundStatistic>(
                String.Format(StatisticKeys.MarketIndexFundByAppId, appId)
            );
            if ((indexFund?.Count ?? 0) <= 0)
            {
                return Ok(Enumerable.Empty<MarketIndexFundChartPointDTO>());
            }

            var salesPerDay = indexFund
                .Where(x => x.Key <= yesterday.Date)
                .Where(x => maxDaysCutoff == null || x.Key >= maxDaysCutoff.Value.Date)
                .OrderBy(x => x.Key)
                .Select(
                    x => new MarketIndexFundChartPointDTO
                    {
                        Date = x.Key,
                        TotalSalesVolume = x.Value.TotalSalesVolume,
                        //AdjustedSalesVolume = (long)Math.Round((x.Value.TotalSalesVolume > 0 && x.Value.TotalItems > 0) ? (x.Value.TotalSalesVolume / (decimal)x.Value.TotalItems) : 0, 0),
                        TotalSalesValue = this.Currency().ToPrice(this.Currency().CalculateExchange((long)x.Value.TotalSalesValue)),
                        AverageItemValue = this.Currency().ToPrice(this.Currency().CalculateExchange((long)Math.Round(x.Value.AverageItemValue, 0))),
                    }
                )
                .ToArray();

            return Ok(salesPerDay);
        }

        /// <summary>
        /// Get the cheapeast market listing for items
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="filter">Optional search filter. Matches against item name, type, or collection</param>
        /// <param name="market">Optional market type filter. If specified, only items from this market will be returned</param>
        /// <param name="minimumPrice">Optional minimum price range filter. If specified, only items priced equal to or higher than this value will be returned</param>
        /// <param name="maximumPrice">Optional maximum price range filter. If specified, only items priced equal to or lower than this value will be returned</param>
        /// <param name="minimumInvestmentReliability"></param>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 100.</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [HttpGet("market/cheapestListings")]
        [ProducesResponseType(typeof(PaginatedResult<MarketItemListingAnalyticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetMarketCheapestListings([FromQuery] string filter = null, [FromQuery] MarketType? market = null, [FromQuery] decimal? minimumPrice = null, [FromQuery] decimal? maximumPrice = null, [FromQuery] decimal? minimumInvestmentReliability = null, [FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var marketTypes = this.User.Preference(_db, x => x.MarketTypes);
            var includeFees = this.User.Preference(_db, x => x.ItemIncludeMarketFees);
            var appId = this.App().Guid;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Where(x => x.AppId == appId)
                .Where(x => String.IsNullOrEmpty(filter) || x.Description.Name.Contains(filter) || x.Description.ItemType.Contains(filter) || x.Description.ItemCollection.Contains(filter))
                .Where(x => market == null || x.BuyPrices.Serialised.Contains(market.ToString()))
                .Select(x => new
                {
                    Id = x.Description.ClassId,
                    AppId = x.App.SteamId,
                    AppName = x.App.Name,
                    IconUrl = x.Description.IconUrl,
                    Name = x.Description.Name,
                    CurrencyExchangeRateMultiplier = x.Currency.ExchangeRateMultiplier,
                    SellOrderLowestPrice = x.SellOrderLowestPrice,
                    BuyPrices = x.BuyPrices,
                    IsBeingManipulated = x.IsBeingManipulated,
                    ManipulationReason = x.ManipulationReason,
                    LastCheckedOrdersOn = x.LastCheckedOrdersOn
                })
                .ToList()
                .Select(x => new
                {
                    Item = x,
                    LowestPrice = x.BuyPrices
                        .Where(p => (market != null && market == p.Key) || (market == null && marketTypes.Contains(p.Key)))
                        .Where(p => p.Value.Price > 0 && p.Value.Supply != 0)
                        .Select(p => new
                        {
                            MarketType = p.Key,
                            Supply = p.Value.Supply,
                            BuyPrice = p.Key.GetBuyFromOptions().FirstOrDefault()?.CalculateBuyPrice(p.Value.Price),
                            BuyFee = p.Key.GetBuyFromOptions().FirstOrDefault()?.CalculateBuyFees(p.Value.Price),
                            BuyUrl = p.Key.GetBuyFromOptions().FirstOrDefault()?.GenerateBuyUrl(x.AppId, x.AppName, x.Id, x.Name)
                        })
                        .OrderBy(p => p.BuyPrice + p.BuyFee)
                        .FirstOrDefault()
                })
                .Where(x => x.LowestPrice != null)
                .Where(x => (minimumPrice == null || minimumPrice == 0 || this.Currency().ToPrice(this.Currency().CalculateExchange(x.LowestPrice.BuyPrice ?? 0, x.Item.CurrencyExchangeRateMultiplier)) >= minimumPrice))
                .Where(x => (maximumPrice == null || maximumPrice == 0 || this.Currency().ToPrice(this.Currency().CalculateExchange(x.LowestPrice.BuyPrice ?? 0, x.Item.CurrencyExchangeRateMultiplier)) <= maximumPrice))
                .Select(x => new MarketItemListingAnalyticDTO()
                {
                    Id = x.Item.Id ?? 0,
                    AppId = ulong.Parse(x.Item.AppId),
                    IconUrl = x.Item.IconUrl,
                    Name = x.Item.Name,
                    BuyFrom = x.LowestPrice.MarketType,
                    BuyPriceLastUpdatedOn = null,
                    BuySupplyAvailable = x.LowestPrice.Supply,
                    BuyPrice = this.Currency().CalculateExchange(x.LowestPrice.BuyPrice ?? 0, x.Item.CurrencyExchangeRateMultiplier),
                    BuyFee = this.Currency().CalculateExchange(x.LowestPrice.BuyFee ?? 0, x.Item.CurrencyExchangeRateMultiplier),
                    BuyUrl = x.LowestPrice.BuyUrl,
                    ReferenceFrom = MarketType.SteamCommunityMarket,
                    ReferencePriceLastUpdatedOn = x.Item.LastCheckedOrdersOn,
                    ReferemcePrice = this.Currency().CalculateExchange(x.Item.SellOrderLowestPrice, x.Item.CurrencyExchangeRateMultiplier),
                    IsBeingManipulated = x.Item.IsBeingManipulated,
                    ManipulationReason = x.Item.ManipulationReason
                })
                .AsQueryable()
                .Where(x => (x.BuyPrice + (includeFees ? x.BuyFee : 0)) <= x.ReferemcePrice)
                .Where(x => (x.BuyPrice + (includeFees ? x.BuyFee : 0)) > 0 && x.ReferemcePrice > 0)
                .OrderByDescending(x => (decimal)(x.ReferemcePrice - (x.BuyPrice + (includeFees ? x.BuyFee : 0))) / (decimal)x.ReferemcePrice);

            count = Math.Max(0, Math.Min(100, count));
            return Ok(
                query.Paginate(start, count)
            );
        }

        /// <summary>
        /// Get items that can be instantly flipped on to the Steam Community Market for profit
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="filter">Optional search filter. Matches against item name, type, or collection</param>
        /// <param name="market">Optional market type filter. If specified, only items from this market will be returned</param>
        /// <param name="minimumPrice">Optional minimum price range filter. If specified, only items priced equal to or higher than this value will be returned</param>
        /// <param name="maximumPrice">Optional maximum price range filter. If specified, only items priced equal to or lower than this value will be returned</param>
        /// <param name="minimumInvestmentReliability"></param>
        /// <param name="sellNow">If true, sell prices are based on highest buy order. If false, sell prices are based on lowest sell order. Default is true.</param>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 100.</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [HttpGet("market/flips")]
        [ProducesResponseType(typeof(PaginatedResult<MarketItemFlipAnalyticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetMarketFlips([FromQuery] string filter = null, [FromQuery] MarketType? market = null, [FromQuery] decimal? minimumPrice = null, [FromQuery] decimal? maximumPrice = null, [FromQuery] decimal? minimumInvestmentReliability = null, [FromQuery] bool sellNow = true, [FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var marketTypes = this.User.Preference(_db, x => x.MarketTypes);
            var includeFees = this.User.Preference(_db, x => x.ItemIncludeMarketFees);
            var appId = this.App().Guid;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Where(x => x.AppId == appId)
                .Where(x => String.IsNullOrEmpty(filter) || x.Description.Name.Contains(filter) || x.Description.ItemType.Contains(filter) || x.Description.ItemCollection.Contains(filter))
                .Where(x => market == null || x.BuyPrices.Serialised.Contains(market.ToString()))
                .Select(x => new
                {
                    Id = x.Description.ClassId,
                    AppId = x.App.SteamId,
                    AppName = x.App.Name,
                    IconUrl = x.Description.IconUrl,
                    Name = x.Description.Name,
                    CurrencyExchangeRateMultiplier = x.Currency.ExchangeRateMultiplier,
                    BuyOrderHighestPrice = x.BuyOrderHighestPrice,
                    SellOrderLowestPrice = x.SellOrderLowestPrice,
                    BuyPrices = x.BuyPrices,
                    IsBeingManipulated = x.IsBeingManipulated,
                    ManipulationReason = x.ManipulationReason,
                    LastCheckedOrdersOn = x.LastCheckedOrdersOn
                })
                .ToList()
                .Select(x => new
                {
                    Item = x,
                    LowestPrice = x.BuyPrices
                        .Where(p => (market != null && market == p.Key) || (market == null && marketTypes.Contains(p.Key)))
                        .Where(p => p.Value.Price > 0 && p.Value.Supply != 0)
                        .Select(p => new
                        {
                            MarketType = p.Key,
                            Supply = p.Value.Supply,
                            BuyPrice = p.Key.GetBuyFromOptions().FirstOrDefault()?.CalculateBuyPrice(p.Value.Price),
                            BuyFee = p.Key.GetBuyFromOptions().FirstOrDefault()?.CalculateBuyFees(p.Value.Price),
                            BuyUrl = p.Key.GetBuyFromOptions().FirstOrDefault()?.GenerateBuyUrl(x.AppId, x.AppName, x.Id, x.Name)
                        })
                        .OrderBy(p => p.BuyPrice + p.BuyFee)
                        .FirstOrDefault()
                })
                .Where(x => x.LowestPrice != null)
                .Where(x => (minimumPrice == null || minimumPrice == 0 || this.Currency().ToPrice(this.Currency().CalculateExchange(x.LowestPrice.BuyPrice ?? 0, x.Item.CurrencyExchangeRateMultiplier)) >= minimumPrice))
                .Where(x => (maximumPrice == null || maximumPrice == 0 || this.Currency().ToPrice(this.Currency().CalculateExchange(x.LowestPrice.BuyPrice ?? 0, x.Item.CurrencyExchangeRateMultiplier)) <= maximumPrice))
                .Select(x => new MarketItemFlipAnalyticDTO()
                {
                    Id = x.Item.Id ?? 0,
                    AppId = ulong.Parse(x.Item.AppId),
                    IconUrl = x.Item.IconUrl,
                    Name = x.Item.Name,
                    BuyFrom = x.LowestPrice.MarketType,
                    BuyPriceLastUpdatedOn = null,
                    BuySupplyAvailable = x.LowestPrice.Supply,
                    BuyPrice = this.Currency().CalculateExchange(x.LowestPrice.BuyPrice ?? 0, x.Item.CurrencyExchangeRateMultiplier),
                    BuyFee = this.Currency().CalculateExchange(x.LowestPrice.BuyFee ?? 0, x.Item.CurrencyExchangeRateMultiplier),
                    BuyUrl = x.LowestPrice.BuyUrl,
                    SellTo = MarketType.SteamCommunityMarket,
                    SellPriceLastUpdatedOn = x.Item.LastCheckedOrdersOn,
                    SellPrice = this.Currency().CalculateExchange(
                        (sellNow ? x.Item.BuyOrderHighestPrice : x.Item.SellOrderLowestPrice),
                        x.Item.CurrencyExchangeRateMultiplier
                    ),
                    SellFee = this.Currency().CalculateExchange(
                        (includeFees ? EconomyExtensions.SteamMarketFeeAsInt(sellNow ? x.Item.BuyOrderHighestPrice : x.Item.SellOrderLowestPrice) : 0),
                        x.Item.CurrencyExchangeRateMultiplier
                    ),
                    IsBeingManipulated = x.Item.IsBeingManipulated,
                    ManipulationReason = x.Item.ManipulationReason
                })
                .AsQueryable()
                .Where(x => (x.BuyPrice + (includeFees ? x.BuyFee : 0)) > 0 && x.SellPrice > 0)
                .Where(x => (x.SellPrice - (x.SellPrice * EconomyExtensions.MarketFeeMultiplier) - (x.BuyPrice + (includeFees ? x.BuyFee : 0))) > 30) // Ignore anything less than 0.30 USD profit, not worth effort
                .OrderByDescending(x => (decimal)(x.SellPrice - ((decimal)(x.SellPrice) * EconomyExtensions.MarketFeeMultiplier) - (x.BuyPrice + (includeFees ? x.BuyFee : 0))) / (decimal)(x.BuyPrice + (includeFees ? x.BuyFee : 0)));

            count = Math.Max(0, Math.Min(100, count));
            return Ok(
                query.Paginate(start, count)
            );
        }

        /// <summary>
        /// Get the cheapest method of acquiring crafting components, sorted by lowest cost
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 100.</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("market/cheapestCraftingResourceCosts")]
        [ProducesResponseType(typeof(PaginatedResult<MarketCraftingItemCostAnalyticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetCraftingCheapestComponentCosts([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var marketTypes = this.User.Preference(_db, x => x.MarketTypes);
            var includeFees = this.User.Preference(_db, x => x.ItemIncludeMarketFees);
            var appId = this.App().Guid;
            var query = _db.SteamAssetDescriptions
                .AsNoTracking()
                .Where(x => x.AppId == appId)
                .Where(x => x.IsCraftingComponent)
                .Where(x => x.MarketItem != null)
                .Select(x => new
                {
                    Resource = new
                    {
                        Id = x.ClassId,
                        AppId = x.App.SteamId,
                        AppName = x.App.Name,
                        BackgroundColour = x.BackgroundColour,
                        ForegroundColour = x.ForegroundColour,
                        IconUrl = x.IconUrl,
                        Name = x.Name,
                        CurrencyExchangeRateMultiplier = x.MarketItem.Currency.ExchangeRateMultiplier,
                        BuyPrices = x.MarketItem.BuyPrices
                    },
                    CheapestBreakdownItem = x.App.AssetDescriptions
                        .Where(y => y.IsBreakable && y.BreaksIntoComponents.Serialised.Contains(x.Name))
                        .Where(y => y.MarketItem != null && y.MarketItem.SellOrderLowestPrice > 0)
                        .OrderBy(y => y.MarketItem.SellOrderLowestPrice)
                        .Select(y => new
                        {
                            Id = y.ClassId,
                            AppId = y.App.SteamId,
                            BackgroundColour = y.BackgroundColour,
                            ForegroundColour = y.ForegroundColour,
                            IconUrl = y.IconUrl,
                            Name = y.Name,
                            CurrencyExchangeRateMultiplier = y.MarketItem.Currency.ExchangeRateMultiplier,
                            BuyNowPrice = y.MarketItem.SellOrderLowestPrice
                        })
                        .FirstOrDefault()
                })
                .ToList()
                .Select(x => new
                {
                    Resource = x.Resource,
                    LowestBuyPrice = x.Resource.BuyPrices
                        .Where(p => p.Key == MarketType.SteamStore || p.Key == MarketType.SteamCommunityMarket || marketTypes.Contains(p.Key))
                        .Where(p => p.Value.Price > 0 && p.Value.Supply != 0)
                        .Select(p => new
                        {
                            MarketType = p.Key,
                            BuyPrice = p.Key.GetBuyFromOptions().FirstOrDefault()?.CalculateBuyPrice(p.Value.Price),
                            BuyFee = p.Key.GetBuyFromOptions().FirstOrDefault()?.CalculateBuyFees(p.Value.Price),
                            BuyUrl = p.Key.GetBuyFromOptions().FirstOrDefault()?.GenerateBuyUrl(x.Resource.AppId, x.Resource.AppName, x.Resource.Id, x.Resource.Name)
                        })
                        .OrderBy(p => p.BuyPrice + p.BuyFee)
                        .FirstOrDefault(),
                    CheapestBreakdownItem = x.CheapestBreakdownItem
                })
                .Where(x => x.LowestBuyPrice != null)
                .Select(x => new MarketCraftingItemCostAnalyticDTO
                {
                    Id = x.Resource.Id ?? 0,
                    AppId = ulong.Parse(x.Resource.AppId),
                    Name = x.Resource.Name,
                    BackgroundColour = x.Resource.BackgroundColour,
                    ForegroundColour = x.Resource.ForegroundColour,
                    IconUrl = x.Resource.IconUrl,
                    BuyFrom = x.LowestBuyPrice.MarketType,
                    BuyPrice = this.Currency().CalculateExchange(x.LowestBuyPrice.BuyPrice ?? 0, x.Resource.CurrencyExchangeRateMultiplier),
                    BuyFee = (includeFees ? this.Currency().CalculateExchange(x.LowestBuyPrice.BuyFee ?? 0, x.Resource.CurrencyExchangeRateMultiplier) : 0),
                    BuyUrl = x.LowestBuyPrice.BuyUrl,
                    CheapestItem = new ItemValueStatisticDTO()
                    {
                        Id = x.CheapestBreakdownItem.Id ?? 0,
                        AppId = ulong.Parse(x.CheapestBreakdownItem.AppId),
                        Name = x.CheapestBreakdownItem.Name,
                        BackgroundColour = x.CheapestBreakdownItem.BackgroundColour,
                        ForegroundColour = x.CheapestBreakdownItem.ForegroundColour,
                        IconUrl = x.CheapestBreakdownItem.IconUrl,
                        BuyNowPrice = this.Currency().CalculateExchange(x.CheapestBreakdownItem.BuyNowPrice, x.CheapestBreakdownItem.CurrencyExchangeRateMultiplier),
                    }
                })
                .AsQueryable()
                .OrderBy(x => x.BuyPrice);

            count = Math.Max(0, Math.Min(100, count));
            return Ok(
                query.Paginate(start, count)
            );
        }

        /// <summary>
        /// Get the cheapest method of acquiring craftable item containers, sorted by lowest cost
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 100.</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("market/cheapestCraftableContainerCosts")]
        [ProducesResponseType(typeof(PaginatedResult<MarketCraftableItemCostAnalyticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetMarketCheapestCraftableContainerCosts([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var marketTypes = this.User.Preference(_db, x => x.MarketTypes);
            var includeFees = this.User.Preference(_db, x => x.ItemIncludeMarketFees);
            var appId = this.App().Guid;
            var resources = _db.SteamAssetDescriptions
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .Where(x => x.AppId == appId)
                .Where(x => x.IsCraftingComponent)
                .Where(x => x.MarketItem != null)
                .Select(x => new
                {
                    Resource = new
                    {
                        Id = x.ClassId,
                        AppId = x.App.SteamId,
                        AppName = x.App.Name,
                        BackgroundColour = x.BackgroundColour,
                        ForegroundColour = x.ForegroundColour,
                        IconUrl = x.IconUrl,
                        Name = x.Name,
                        CurrencyExchangeRateMultiplier = x.MarketItem.Currency.ExchangeRateMultiplier,
                        BuyPrices = x.MarketItem.BuyPrices
                    },
                    CheapestBreakdownItem = x.App.AssetDescriptions
                        .Where(y => y.IsBreakable && y.BreaksIntoComponents.Serialised.Contains(x.Name))
                        .Where(y => y.MarketItem != null && y.MarketItem.SellOrderLowestPrice > 0)
                        .OrderBy(y => y.MarketItem.SellOrderLowestPrice)
                        .Select(y => new
                        {
                            Id = y.ClassId,
                            AppId = y.App.SteamId,
                            BackgroundColour = y.BackgroundColour,
                            ForegroundColour = y.ForegroundColour,
                            IconUrl = y.IconUrl,
                            Name = y.Name,
                            CurrencyExchangeRateMultiplier = y.MarketItem.Currency.ExchangeRateMultiplier,
                            BuyNowPrice = y.MarketItem.SellOrderLowestPrice
                        })
                        .FirstOrDefault()
                })
                .ToList()
                .Select(x => new
                {
                    Resource = x.Resource,
                    LowestBuyPrice = x.Resource.BuyPrices
                        .Where(p => p.Key == MarketType.SteamStore || p.Key == MarketType.SteamCommunityMarket || marketTypes.Contains(p.Key))
                        .Where(p => p.Value.Price > 0 && p.Value.Supply != 0)
                        .Select(p => new
                        {
                            MarketType = p.Key,
                            BuyPrice = p.Key.GetBuyFromOptions().FirstOrDefault()?.CalculateBuyPrice(p.Value.Price),
                            BuyFee = p.Key.GetBuyFromOptions().FirstOrDefault()?.CalculateBuyFees(p.Value.Price),
                            BuyUrl = p.Key.GetBuyFromOptions().FirstOrDefault()?.GenerateBuyUrl(x.Resource.AppId, x.Resource.AppName, x.Resource.Id, x.Resource.Name)
                        })
                        .OrderBy(p => p.BuyPrice + p.BuyFee)
                        .FirstOrDefault(),
                    CheapestBreakdownItem = x.CheapestBreakdownItem
                });

            var query = _db.SteamAssetDescriptions
                .AsNoTracking()
                .Where(x => x.AppId == appId)
                .Where(x => x.IsCraftable)
                .Where(x => x.MarketItem != null)
                .Select(x => new
                {
                    Container = new
                    {
                        Id = x.ClassId,
                        AppId = x.App.SteamId,
                        AppName = x.App.Name,
                        BackgroundColour = x.BackgroundColour,
                        ForegroundColour = x.ForegroundColour,
                        IconUrl = x.IconUrl,
                        Name = x.Name,
                        CurrencyExchangeRateMultiplier = x.MarketItem.Currency.ExchangeRateMultiplier,
                        BuyPrices = x.MarketItem.BuyPrices,
                        CraftingComponents = x.CraftingComponents
                    }
                })
                .ToList()
                .Select(x => new
                {
                    Container = x.Container,
                    LowestBuyPrice = x.Container.BuyPrices
                        .Where(p => p.Key == MarketType.SteamStore || p.Key == MarketType.SteamCommunityMarket || marketTypes.Contains(p.Key))
                        .Where(p => p.Value.Price > 0 && p.Value.Supply != 0)
                        .Select(p => new
                        {
                            MarketType = p.Key,
                            BuyPrice = p.Key.GetBuyFromOptions().FirstOrDefault()?.CalculateBuyPrice(p.Value.Price),
                            BuyFee = p.Key.GetBuyFromOptions().FirstOrDefault()?.CalculateBuyFees(p.Value.Price),
                            BuyUrl = p.Key.GetBuyFromOptions().FirstOrDefault()?.GenerateBuyUrl(x.Container.AppId, x.Container.AppName, x.Container.Id, x.Container.Name)
                        })
                        .OrderBy(p => p.BuyPrice + p.BuyFee)
                        .FirstOrDefault(),
                })
                .Where(x => x.LowestBuyPrice != null)
                .Select(x => new MarketCraftableItemCostAnalyticDTO
                {
                    Id = x.Container.Id ?? 0,
                    AppId = ulong.Parse(x.Container.AppId),
                    Name = x.Container.Name,
                    BackgroundColour = x.Container.BackgroundColour,
                    ForegroundColour = x.Container.ForegroundColour,
                    IconUrl = x.Container.IconUrl,
                    BuyFrom = x.LowestBuyPrice.MarketType,
                    BuyPrice = this.Currency().CalculateExchange(x.LowestBuyPrice.BuyPrice ?? 0, x.Container.CurrencyExchangeRateMultiplier),
                    BuyFee = (includeFees ? this.Currency().CalculateExchange(x.LowestBuyPrice.BuyFee ?? 0, x.Container.CurrencyExchangeRateMultiplier) : 0),
                    BuyUrl = x.LowestBuyPrice.BuyUrl,
                    CraftingComponents = x.Container.CraftingComponents
                        .Join(resources, x => x.Key, x => x.Resource.Name, (x, y) => new
                        {
                            Resource = y.Resource,
                            Quantity = x.Value,
                            CheapestBuyNowPrice = (y.LowestBuyPrice.BuyPrice <= y.CheapestBreakdownItem.BuyNowPrice) ? y.LowestBuyPrice.BuyPrice : y.CheapestBreakdownItem.BuyNowPrice,
                        })
                        .Select(y => new ItemCraftingComponentCostDTO()
                        {
                            Name = y.Resource.Name,
                            Quantity = y.Quantity,
                            Component = new ItemValueStatisticDTO
                            {
                                Id = y.Resource.Id ?? 0,
                                AppId = ulong.Parse(y.Resource.AppId),
                                Name = y.Resource.Name,
                                BackgroundColour = y.Resource.BackgroundColour,
                                ForegroundColour = y.Resource.ForegroundColour,
                                IconUrl = y.Resource.IconUrl,
                                BuyNowPrice = this.Currency().CalculateExchange(y.CheapestBuyNowPrice ?? 0, y.Resource.CurrencyExchangeRateMultiplier)
                            }
                        })
                        .ToArray()
                })
                .AsQueryable()
                .OrderBy(x => x.BuyPrice);

            count = Math.Max(0, Math.Min(100, count));
            return Ok(
                query.Paginate(start, count)
            );
        }

        /// 
        /// <summary>
        /// Get marketplace listing activity from the last 24hrs
        /// </summary>
        /// <param name="type">Optional filter, matches against the activity event type</param>
        /// <param name="user">Optional filter, matches against the buyer name or seller name</param>
        /// <param name="item">Optional filter, matches against the item name</param>
        /// <param name="start">Return activity starting at this specific index (pagination)</param>
        /// <param name="count">Number activity to be returned (can be less if not enough data). Max 100.</param>
        /// <response code="200">Paginated list of activity matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("market/activity")]
        [ProducesResponseType(typeof(PaginatedResult<ItemActivityStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMarketActivity([FromQuery] SteamMarketItemActivityType? type = null, [FromQuery] string user = null, [FromQuery] string item = null, [FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var appId = this.App().Guid;
            var query = _db.SteamMarketItemActivity
                .AsNoTracking()
                .Include(x => x.Description).ThenInclude(x => x.App)
                .Include(x => x.Currency)
                .Where(x => x.Item.AppId == appId)
                .Where(x => type == null || x.Type == type)
                .Where(x => string.IsNullOrEmpty(user) || x.BuyerName.Contains(user) || x.SellerName.Contains(user))
                .Where(x => string.IsNullOrEmpty(item) || x.Description.Name.Contains(item))
                .OrderByDescending(x => x.Timestamp);

            count = Math.Max(0, Math.Min(100, count));
            return Ok(
                await query.PaginateAsync(start, count, x => new ItemActivityStatisticDTO()
                {
                    Id = x.Description.ClassId ?? 0,
                    AppId = ulong.Parse(x.Description.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Timestamp = x.Timestamp,
                    Type = x.Type,
                    Price = this.Currency().CalculateExchange(x.Price, x.Currency),
                    Quantity = x.Quantity,
                    SellerName = x.SellerName,
                    SellerAvatarUrl = x.SellerAvatarUrl,
                    BuyerName = x.BuyerName,
                    BuyerAvatarUrl = x.BuyerAvatarUrl
                })
            );
        }

        /// <summary>
        /// Get store item top seller changes timeline
        /// </summary>
        /// <param name="start">Starting from date</param>
        /// <param name="end">Ending at date</param>
        /// <response code="200">The store item top seller changes timeline.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("store/topSellers")]
        [ProducesResponseType(typeof(IEnumerable<StoreTopSellerItemDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStoreItemTopSellersTimeline([FromQuery] long start, [FromQuery] long? end)
        {
            var app = this.App();
            var startDate = new DateTimeOffset(start, TimeZoneInfo.Utc.BaseUtcOffset);
            var endDate = (end != null ? new DateTimeOffset(end.Value, TimeZoneInfo.Utc.BaseUtcOffset) : (DateTimeOffset?)null);
            var topSellerPositions = await _db.SteamStoreItemTopSellerPositions
                .Where(x => x.Description.AppId == app.Guid)
                .Where(x => x.Timestamp >= startDate)
                .Where(x => endDate == null || x.Timestamp <= endDate)
                .Select(x => new
                {
                    Name = x.Description.Name,
                    IconUrl = x.Description.IconUrl,
                    IconAccentColour = x.Description.IconAccentColour,
                    Timestamp = x.Timestamp,
                    Position = x.Position,
                    Total = x.Total,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            return Ok(
                topSellerPositions
                    .GroupBy(x => new { x.Name, x.IconUrl, x.IconAccentColour })
                    .Select(x => new StoreTopSellerItemDTO
                    {
                        Name = x.Key.Name,
                        IconUrl = x.Key.IconUrl,
                        IconAccentColour = x.Key.IconAccentColour,
                        Position = x.FirstOrDefault(x => x.IsActive)?.Position ?? 0,
                        PositionChanges = x.Select(p => new StoreTopSellerPositionChartPointDTO
                        {
                            Timestamp = p.Timestamp.UtcDateTime,
                            Position = p.Position,
                            Total = p.Total,
                            IsActive = p.IsActive
                        }).ToList()
                    })
                    .ToArray()
            );
        }

        /// <summary>
        /// Get distribution of item types from accepted items and workshop submissions
        /// </summary>
        /// <param name="fromYear">If specified, only items created on or after this year will be counted</param>
        /// <param name="toYear">If specified, only items created on or before this year will be counted</param>
        /// <response code="200">List of market index fund values grouped/keyed per day by UTC date.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/typeDistribution")]
        [ProducesResponseType(typeof(IEnumerable<ItemTypeDistributionChartPointDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsTypeDistribution([FromQuery] int? fromYear = null, [FromQuery] int? toYear = null)
        {
            var appId = this.App().Guid;
            fromYear = (fromYear ?? DateTime.MinValue.Year);
            toYear = (toYear ?? DateTime.Now.Year);

            var itemTypeDistribution = await _db.SteamAssetDescriptions.AsNoTracking()
                .Where(x => x.AppId == appId)
                .Where(x => x.IsAccepted)
                .Where(x => x.TimeAccepted != null && x.TimeAccepted.Value.Year >= fromYear.Value && x.TimeAccepted.Value.Year <= toYear)
                .GroupBy(x => x.ItemType)
                .Where(x => x.Key != null)
                .Select(x => new ItemTypeDistributionChartPointDTO
                {
                    ItemType = x.Key,
                    Submitted = _db.SteamWorkshopFiles
                        .Where(y => y.AppId == appId)
                        .Where(y => y.ItemType == x.Key)
                        .Where(y => y.TimeCreated != null && y.TimeCreated.Value.Year >= fromYear.Value && y.TimeCreated.Value.Year <= toYear)
                        .Count(),
                    Accepted = x.Count(y => y.IsAccepted),
                })
                .ToListAsync();

            return Ok(itemTypeDistribution
                .OrderByDescending(x => x.Accepted)
                .ToArray()
            );
        }

        /// <summary>
        /// List items, sorted by highest number of sales in the last 24hrs
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 100.</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/mostDemanded")]
        [ProducesResponseType(typeof(PaginatedResult<ItemSupplyDemandStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsMostDemanded([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var appId = this.App().Guid;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Description)
                .Where(x => x.AppId == appId)
                .Where(x => x.Last24hrSales > 0)
                .OrderByDescending(x => x.Last24hrSales)
                .Select(x => new ItemSupplyDemandStatisticDTO()
                {
                    Id = x.Description.ClassId ?? 0,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Supply = x.SellOrderCount,
                    Demand = x.Last24hrSales
                });

            count = Math.Max(0, Math.Min(100, count));
            return Ok(
                await query.PaginateAsync(start, count)
            );
        }

        /// <summary>
        /// List items, sorted by highest supply
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 100</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/mostSaturated")]
        [ProducesResponseType(typeof(PaginatedResult<ItemSupplyDemandStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsMostSaturated([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var appId = this.App().Guid;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Description)
                .Where(x => x.AppId == appId)
                .Where(x => x.SellOrderCount > 0)
                .OrderByDescending(x => x.SellOrderCount)
                .Select(x => new ItemSupplyDemandStatisticDTO()
                {
                    Id = x.Description.ClassId ?? 0,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    Supply = x.SellOrderCount,
                    Demand = x.Last24hrSales
                });

            count = Math.Max(0, Math.Min(100, count));
            return Ok(
                await query.PaginateAsync(start, count)
            );
        }

        /// <summary>
        /// List items currently at their all-time highest value, sorted by highest value
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 100.</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/allTimeHigh")]
        [ProducesResponseType(typeof(PaginatedResult<ItemValueStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsAllTimeHigh([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var lastFewHours = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromHours(6));
            var appId = this.App().Guid;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.AppId == appId)
                .Where(x => x.LastCheckedSalesOn >= lastFewHours && x.LastCheckedOrdersOn >= lastFewHours)
                .Where(x => x.SellOrderCount > 0)
                .Where(x => x.AllTimeHighestValue > 0)
                .Where(x => x.SellOrderLowestPrice >= x.AllTimeHighestValue)
                .OrderBy(x => x.AllTimeHighestValueOn);

            count = Math.Max(0, Math.Min(100, count));
            return Ok(
                await query.PaginateAsync(start, count, x => new ItemValueStatisticDTO()
                {
                    Id = x.Description.ClassId ?? 0,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    BuyNowPrice = this.Currency().CalculateExchange(x.SellOrderLowestPrice, x.Currency)
                })
            );
        }

        /// <summary>
        /// List items current at their all-time lowest value, sorted by lowest value
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 100.</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/allTimeLow")]
        [ProducesResponseType(typeof(PaginatedResult<ItemValueStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsAllTimeLow([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var lastFewHours = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromHours(6));
            var appId = this.App().Guid;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.AppId == appId)
                .Where(x => x.LastCheckedSalesOn >= lastFewHours && x.LastCheckedOrdersOn >= lastFewHours)
                .Where(x => x.SellOrderCount > 0)
                .Where(x => x.AllTimeLowestValue > 0)
                .Where(x => x.SellOrderLowestPrice <= x.AllTimeLowestValue)
                .OrderBy(x => x.AllTimeLowestValueOn);

            count = Math.Max(0, Math.Min(100, count));
            return Ok(
                await query.PaginateAsync(start, count, x => new ItemValueStatisticDTO()
                {
                    Id = x.Description.ClassId ?? 0,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    BuyNowPrice = this.Currency().CalculateExchange(x.SellOrderLowestPrice, x.Currency)
                })
            );
        }

        /// <summary>
        /// List items, sorted by highest value
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 100.</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/mostExpensive")]
        [ProducesResponseType(typeof(PaginatedResult<ItemValueStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsMostExpensive([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var appId = this.App().Guid;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.AppId == appId)
                .Where(x => x.SellOrderLowestPrice > 0)
                .OrderByDescending(x => x.SellOrderLowestPrice);

            count = Math.Max(0, Math.Min(100, count));
            return Ok(
                await query.PaginateAsync(start, count, x => new ItemValueStatisticDTO()
                {
                    Id = x.Description.ClassId ?? 0,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Description.Name,
                    BackgroundColour = x.Description.BackgroundColour,
                    ForegroundColour = x.Description.ForegroundColour,
                    IconUrl = x.Description.IconUrl,
                    BuyNowPrice = this.Currency().CalculateExchange(x.SellOrderLowestPrice, x.Currency)
                })
            );
        }

        /// <summary>
        /// List items, sorted by highest estimated total supply
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 100.</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/mostSupply")]
        [ProducesResponseType(typeof(PaginatedResult<ItemSupplyStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetItemsMostSupply([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var appId = this.App().Guid;
            var query = _db.SteamAssetDescriptions
                .AsNoTracking()
                .Include(x => x.App)
                .Where(x => x.AppId == appId)
                .Where(x => x.WorkshopFileId > 0 && x.SupplyTotalEstimated > 0)
                .Where(x => x.IsMarketable)
                .OrderByDescending(x => x.SupplyTotalEstimated)
                .Select(x => new ItemSupplyStatisticDTO()
                {
                    Id = x.ClassId ?? 0,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Name,
                    BackgroundColour = x.BackgroundColour,
                    ForegroundColour = x.ForegroundColour,
                    IconUrl = x.IconUrl,
                    SupplyTotalEstimated = x.SupplyTotalEstimated.Value
                });

            count = Math.Max(0, Math.Min(100, count));
            return Ok(
                await query.PaginateAsync(start, count)
            );
        }

        /// <summary>
        /// List items, grouped by collection name, sorted by highest item count
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 100.</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("items/largestCollections")]
        [ProducesResponseType(typeof(PaginatedResult<DashboardAssetCollectionDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetItemsLargestCollections([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var appId = this.App().Guid;
            var query = _db.SteamAssetDescriptions
                .AsNoTracking()
                .Include(x => x.App)
                .Where(x => x.AppId == appId)
                .Where(x => x.ItemCollection != null)
                .Select(x => new
                {
                    CreatorId = (!x.IsPublisherDrop && !x.IsTwitchDrop) ? x.CreatorId : null,
                    Name = x.ItemCollection,
                    IconUrl = x.IconUrl,
                    // NOTE: This isn't 100% accurate if the store item price is used. Update this to use StoreItem.Prices with the local currency
                    BuyNowPrice = x.MarketItem != null ? (long?)x.MarketItem.SellOrderLowestPrice : (x.StoreItem != null ? (long?)x.StoreItem.Price : null),
                    Currency = x.MarketItem != null ? x.MarketItem.Currency : (x.StoreItem != null ? x.StoreItem.Currency : null)
                })
                .ToList()
                .GroupBy(x => new
                {
                    CreatorId = x.CreatorId,
                    Name = x.Name
                })
                .OrderByDescending(x => x.Count())
                .ThenByDescending(x => x.Sum(y => y.BuyNowPrice ?? 0))
                .AsQueryable();

            count = Math.Max(0, Math.Min(100, count));
            return Ok(
                query.Paginate(start, count, x => new DashboardAssetCollectionDTO
                {
                    CreatorId = x.Key.CreatorId,
                    Name = x.Key.Name,
                    IconUrl = x.FirstOrDefault(y => y.BuyNowPrice == x.Max(z => z.BuyNowPrice))?.IconUrl,
                    Items = x.Count(),
                    BuyNowPrice = this.Currency().CalculateExchange(x.Sum(y => y.BuyNowPrice ?? 0), x.FirstOrDefault()?.Currency)
                })
            );
        }

        /// <summary>
        /// List profiles with accepted workshop items, sorted by highest number of items
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 100.</param>
        /// <response code="200">Paginated list of profiles matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("profiles/largestCreators")]
        [ProducesResponseType(typeof(PaginatedResult<ProfileAcceptedItemsStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfilesLargestCreators([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var appId = this.App().Guid;
            var query = _db.SteamProfiles
                .AsNoTracking()
                .Where(x => x.AssetDescriptions.Count(y => y.AppId == appId && y.TimeAccepted != null) > 0)
                .OrderByDescending(x => x.AssetDescriptions.Count(y => y.AppId == appId && y.TimeAccepted != null))
                .Select(x => new ProfileAcceptedItemsStatisticDTO()
                {
                    SteamId = x.SteamId,
                    Name = x.Name,
                    AvatarUrl = x.AvatarUrl,
                    Items = x.AssetDescriptions.Count(y => y.TimeAccepted != null),
                });

            count = Math.Max(0, Math.Min(100, count));
            return Ok(
                await query.PaginateAsync(start, count)
            );
        }

        /// <summary>
        /// Get profile inventory total statistics
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// Response is cached for 1hr.
        /// </remarks>
        /// <response code="200">The totals across all profile inventories.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("profiles/inventories/total")]
        [ProducesResponseType(typeof(ProfileInventoryTotalsStatisticDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OutputCache(PolicyName = CachePolicy.Expire1h, Tags = [CacheTag.Statistics])]
        public async Task<IActionResult> GetProfileInventoriesTotal()
        {
            var appId = this.App().Guid;
            var totals = await _db.SteamProfileInventoryValues
                .AsNoTracking()
                .Where(x => x.AppId == appId)
                .Where(x => x.Items > 0)
                .GroupBy(x => true)
                .Select(x => new
                {
                    Inventories = x.Count(),
                    Items = x.Sum(i => i.Items),
                    MarketValue = x.Sum(i => i.MarketValue)
                })
                .FirstOrDefaultAsync();

            return Ok(new ProfileInventoryTotalsStatisticDTO()
            {
                TotalInventories = totals?.Inventories ?? 0,
                TotalItems = totals?.Items ?? 0,
                TotalMarketValue = this.Currency().CalculateExchange(totals?.MarketValue ?? 0)
            });
        }

        /// <summary>
        /// Get your profile inventory rank
        /// </summary>
        /// <remarks>This API requires authentication</remarks>
        /// <response code="200">The rank of your inventory.</response>
        /// <response code="401">If the request is unauthenticated (login first).</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(AuthorizationPolicies.User)]
        [HttpGet("profiles/inventories/myRank")]
        [ProducesResponseType(typeof(ProfileInventoryValueStatisticDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfilesInventoriesMyLeaderboardRank()
        {
            var appId = this.App().Guid;
            var profileId = this.User.Id();
            var ranking = await _db.SteamProfileInventoryValues
                .AsNoTracking()
                .Where(x => x.AppId == appId)
                .Where(x => x.ProfileId == profileId)
                .Select(x => new ProfileInventoryValueStatisticDTO()
                {
                    Rank = 1 + _db.SteamProfileInventoryValues
                        .Where(y => y.AppId == appId)
                        .Where(y => y.MarketValue > x.MarketValue)
                        .Count(),
                    SteamId = x.Profile.SteamId,
                    Name = x.Profile.Name,
                    AvatarUrl = x.Profile.AvatarUrl,
                    IsPrivate = x.Profile.Privacy != SteamVisibilityType.Public,
                    IsBanned = x.Profile.IsTradeBanned,
                    IsBot = x.Profile.Roles.Serialised.Contains(Roles.Bot),
                    Items = x.Items,
                    Value = x.MarketValue,
                    LastUpdatedOn = x.Profile.LastUpdatedInventoryOn
                })
                .SingleOrDefaultAsync();

            ranking.Value = this.Currency().CalculateExchange(ranking.Value);
            return Ok(ranking);
        }

        /// <summary>
        /// List profiles inventory values, sorted by highest value first
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data). Max 100.</param>
        /// <response code="200">Paginated list of profiles matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("profiles/inventories/highestValue")]
        [ProducesResponseType(typeof(PaginatedResult<ProfileInventoryValueStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfilesInventoriesByHighestValue([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var appId = this.App().Guid;
            var query = _db.SteamProfileInventoryValues
                .AsNoTracking()
                .Where(x => x.AppId == appId)
                .Where(x => x.MarketValue > 0)
                .OrderByDescending(x => x.MarketValue)
                .Select(x => new ProfileInventoryValueStatisticDTO()
                {
                    SteamId = (x.Profile.ItemAnalyticsParticipation == ItemAnalyticsParticipationType.Public) ? x.Profile.SteamId : null,
                    Name = (x.Profile.ItemAnalyticsParticipation == ItemAnalyticsParticipationType.Public) ? x.Profile.Name : null,
                    AvatarUrl = (x.Profile.ItemAnalyticsParticipation == ItemAnalyticsParticipationType.Public) ? x.Profile.AvatarUrl : null,
                    IsPrivate = (x.Profile.Privacy != SteamVisibilityType.Public),
                    IsBanned = x.Profile.IsTradeBanned,
                    IsBot = x.Profile.Roles.Serialised.Contains(Roles.Bot),
                    Items = x.Items,
                    Value = x.MarketValue,
                    LastUpdatedOn = x.Profile.LastUpdatedInventoryOn
                });

            count = Math.Max(0, Math.Min(100, count));
            var profiles = await query.PaginateAsync(start, count, x =>
            {
                x.Value = this.Currency().CalculateExchange(x.Value);
                return x;
            });

            // Calculate rank positions
            for (int i = 0; i < profiles.Items.Length; i++)
            {
                profiles.Items[i].Rank = (start + i + 1);
            }

            return Ok(profiles);
        }

        /// <summary>
        /// List profiles inventory values, sorted by most recently updated first
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of profiles matching the request parameters. Max 100.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("profiles/inventories/recentlyValued")]
        [Obsolete("This is no longer used in the web app")]
        [ProducesResponseType(typeof(PaginatedResult<ProfileInventoryValueStatisticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfilesInventoriesByMostRecentlyValued([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var appId = this.App().Guid;
            var query = _db.SteamProfileInventoryValues
                .AsNoTracking()
                .Where(x => x.AppId == appId)
                .Where(x => x.MarketValue > 0)
                .OrderByDescending(x => x.Profile.LastUpdatedInventoryOn)
                .Select(x => new ProfileInventoryValueStatisticDTO()
                {
                    SteamId = (x.Profile.ItemAnalyticsParticipation == ItemAnalyticsParticipationType.Public) ? x.Profile.SteamId : null,
                    Name = (x.Profile.ItemAnalyticsParticipation == ItemAnalyticsParticipationType.Public) ? x.Profile.Name : null,
                    AvatarUrl = (x.Profile.ItemAnalyticsParticipation == ItemAnalyticsParticipationType.Public) ? x.Profile.AvatarUrl : null,
                    IsPrivate = (x.Profile.Privacy != SteamVisibilityType.Public),
                    IsBanned = x.Profile.IsTradeBanned,
                    IsBot = x.Profile.Roles.Serialised.Contains(Roles.Bot),
                    Items = x.Items,
                    Value = x.MarketValue,
                    LastUpdatedOn = x.Profile.LastUpdatedInventoryOn
                });

            count = Math.Max(0, Math.Min(100, count));
            return Ok(
                await query.PaginateAsync(start, count, x =>
                {
                    x.Value = this.Currency().CalculateExchange(x.Value);
                    return x;
                })
            );
        }

        /// <summary>
        /// List profiles who have the donator role, sorted by highest contribution
        /// </summary>
        /// <remarks>Response is cached for 1hr</remarks>
        /// <returns>The list of profiles who have donated</returns>
        /// <response code="200">The list of profiles who have donated.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("donators")]
        [ProducesResponseType(typeof(IEnumerable<ProfileDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OutputCache(PolicyName = CachePolicy.Expire1h, Tags = [CacheTag.Statistics])]
        public async Task<IActionResult> GetProfilesDonators()
        {
            var donators = await _db.SteamProfiles
                .AsNoTracking()
                .Where(x => x.DonatorLevel > 0)
                .OrderByDescending(x => x.DonatorLevel)
                .Select(x => _mapper.Map<ProfileDTO>(x))
                .ToArrayAsync();

            return Ok(donators);
        }

        /// <summary>
        /// List profiles who have the contributor role
        /// </summary>
        /// <remarks>Response is cached for 1hr</remarks>
        /// <returns>The list of profiles who have contributed</returns>
        /// <response code="200">The list of profiles who have contributed.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("contributors")]
        [ProducesResponseType(typeof(IEnumerable<ProfileDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OutputCache(PolicyName = CachePolicy.Expire1h, Tags = [CacheTag.Statistics])]
        public async Task<IActionResult> GetProfilesContributors()
        {
            var donators = await _db.SteamProfiles
                .AsNoTracking()
                .Where(x => x.Roles.Serialised.Contains(Roles.Contributor))
                .OrderByDescending(x => x.SteamId)
                .Select(x => _mapper.Map<ProfileDTO>(x))
                .ToArrayAsync();

            return Ok(donators);
        }
    }
}
