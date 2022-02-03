﻿using AutoMapper;
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
using SCMM.Web.Data.Models.UI.Statistic;
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
        /// Get marketplace sales and revenue chart data, grouped by day (UTC)
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="maxDays">The maximum number of days worth of market history to return. Use <code>-1</code> for all history</param>
        /// <response code="200">List of market sales and revenue grouped/keyed per day by UTC date.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("market/sales")]
        [ProducesResponseType(typeof(IEnumerable<MarketSalesChartPointDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMarketSales(int maxDays = 30)
        {
            var yesterday = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var maxDaysCutoff = (maxDays >= 1 ? DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(maxDays)) : (DateTimeOffset?)null);
            var appId = this.App().Guid;
            var query = _db.SteamMarketItemSale
                .AsNoTracking()
                .Where(x => x.Item.AppId == appId)
                .Where(x => x.Timestamp.Date <= yesterday.Date)
                .Where(x => maxDaysCutoff == null || x.Timestamp.Date >= maxDaysCutoff.Value.Date)
                .GroupBy(x => x.Timestamp.Date)
                .OrderBy(x => x.Key.Date)
                .Select(x => new
                {
                    // TODO: Snapshot these for faster querying
                    Date = x.Key,
                    Volume = x.Sum(y => y.Quantity),
                    Revenue = x.Sum(y => y.Quantity * y.MedianPrice)
                });

            var salesPerDay = (await query.ToListAsync()).Select(
                x => new MarketSalesChartPointDTO
                {
                    Date = x.Date,
                    Volume = x.Volume,
                    Revenue = this.Currency().ToPrice(this.Currency().CalculateExchange(x.Revenue))
                }
            );

            return Ok(salesPerDay);
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
            var appId = this.App().Guid;
            var query = _db.SteamMarketItemSale
                .AsNoTracking()
                .Where(x => x.Item.AppId == appId)
                .Where(x => x.Timestamp.Date <= yesterday.Date)
                .Where(x => maxDaysCutoff == null || x.Timestamp.Date >= maxDaysCutoff.Value.Date)
                .GroupBy(x => x.Timestamp.Date)
                .OrderBy(x => x.Key.Date)
                .Select(x => new
                {
                    // TODO: Snapshot these for faster querying
                    Date = x.Key,
                    Volume = x/*.DistinctBy(x => x.ItemId)*/.Sum(y => y.Quantity),
                    MedianPrice = x.Average(y => y.MedianPrice),
                });

            var salesPerDay = (await query.ToListAsync()).Select(
                x => new MarketIndexFundChartPointDTO
                {
                    Date = x.Date,
                    Volume = x.Volume/*(long)Math.Round((x.Volume > 0 && x.ItemCount > 0) ? (x.Volume / (decimal)x.ItemCount) : 0, 0)*/,
                    Value = this.Currency().ToPrice(this.Currency().CalculateExchange((long)Math.Round(x.MedianPrice, 0)))
                }
            );

            return Ok(salesPerDay);
        }

        /// <summary>
        /// Get items that can be instantly flipped on to the Steam Community Market for profit
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="filter">Optional search filter. Matches against item name, type, or collection</param>
        /// <param name="sellNow">If true, sell prices are based on highest buy order. If false, sell prices are based on lowest sell order. Default is true.</param>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [HttpGet("market/flips")]
        [ProducesResponseType(typeof(PaginatedResult<MarketItemFlipAnalyticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMarketFlips([FromQuery] string filter = null, [FromQuery] bool sellNow = true, [FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var includeFees = this.User.Preference(_db, x => x.ItemIncludeMarketFees);
            var appId = this.App().Guid;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.AppId == appId)
                .Where(x => String.IsNullOrEmpty(filter) || x.Description.Name.Contains(filter) || x.Description.ItemType.Contains(filter) || x.Description.ItemCollection.Contains(filter))
                .Where(x => (x.BuyNowPrice + (includeFees ? x.BuyNowFee : 0)) > 0 && (sellNow ? x.BuyOrderHighestPrice : x.SellOrderLowestPrice) > 0)
                .Where(x => ((sellNow ? x.BuyOrderHighestPrice : x.SellOrderLowestPrice) - ((sellNow ? x.BuyOrderHighestPrice : x.SellOrderLowestPrice) * EconomyExtensions.MarketFeeMultiplier) - (x.BuyNowPrice + (includeFees ? x.BuyNowFee : 0))) > 1)
                .OrderByDescending(x => (decimal)((sellNow ? x.BuyOrderHighestPrice : x.SellOrderLowestPrice) - ((decimal)(sellNow ? x.BuyOrderHighestPrice : x.SellOrderLowestPrice) * EconomyExtensions.MarketFeeMultiplier) - (x.BuyNowPrice + (includeFees ? x.BuyNowFee : 0))) / (decimal)(x.BuyNowPrice + (includeFees ? x.BuyNowFee : 0)));

            return Ok(
                await query.PaginateAsync(start, count, x => new MarketItemFlipAnalyticDTO()
                {
                    Id = x.Description.ClassId ?? 0,
                    AppId = ulong.Parse(x.App.SteamId),
                    IconUrl = x.Description.IconUrl,
                    Name = x.Description.Name,
                    BuyFrom = x.BuyNowFrom,
                    BuyPrice = this.Currency().CalculateExchange(x.BuyNowPrice, x.Currency),
                    BuyFee = (includeFees ? this.Currency().CalculateExchange(x.BuyNowFee, x.Currency) : 0),
                    BuyUrl = x.Description.GetBuyPrices(x.Currency)?.FirstOrDefault(p => p.MarketType == x.BuyNowFrom)?.Url,
                    SellTo = MarketType.SteamCommunityMarket,
                    SellPrice = this.Currency().CalculateExchange(sellNow ? x.BuyOrderHighestPrice : x.SellOrderLowestPrice, x.Currency),
                    SellFee = (includeFees ? this.Currency().CalculateExchange(EconomyExtensions.SteamMarketFeeAsInt(sellNow ? x.BuyOrderHighestPrice : x.SellOrderLowestPrice), x.Currency) : 0),
                })
            );
        }

        /// <summary>
        /// Get the cheapeast market listing for items
        /// </summary>
        /// <remarks>
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <param name="filter">Optional search filter. Matches against item name, type, or collection</param>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [HttpGet("market/cheapestListings")]
        [ProducesResponseType(typeof(PaginatedResult<MarketItemListingAnalyticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMarketCheapestListings([FromQuery] string filter = null, [FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var includeFees = this.User.Preference(_db, x => x.ItemIncludeMarketFees);
            var appId = this.App().Guid;
            var query = _db.SteamMarketItems
                .AsNoTracking()
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.AppId == appId)
                .Where(x => String.IsNullOrEmpty(filter) || x.Description.Name.Contains(filter) || x.Description.ItemType.Contains(filter) || x.Description.ItemCollection.Contains(filter))
                .Where(x => (x.BuyNowPrice + (includeFees ? x.BuyNowFee : 0)) <= x.SellOrderLowestPrice)
                .Where(x => (x.BuyNowPrice + (includeFees ? x.BuyNowFee : 0)) > 0 && x.SellOrderLowestPrice > 0)
                .OrderByDescending(x => (decimal)(x.SellOrderLowestPrice - (x.BuyNowPrice + (includeFees ? x.BuyNowFee : 0))) / (decimal)x.SellOrderLowestPrice);

            return Ok(
                await query.PaginateAsync(start, count, x => new MarketItemListingAnalyticDTO()
                {
                    Id = x.Description.ClassId ?? 0,
                    AppId = ulong.Parse(x.App.SteamId),
                    IconUrl = x.Description.IconUrl,
                    Name = x.Description.Name,
                    BuyFrom = x.BuyNowFrom,
                    BuyPrice = this.Currency().CalculateExchange(x.BuyNowPrice, x.Currency),
                    BuyFee = (includeFees ? this.Currency().CalculateExchange(x.BuyNowFee, x.Currency) : 0),
                    BuyUrl = x.Description.GetBuyPrices(x.Currency)?.FirstOrDefault(p => p.MarketType == x.BuyNowFrom)?.Url,
                    ReferenceFrom = MarketType.SteamCommunityMarket,
                    ReferemcePrice = this.Currency().CalculateExchange(x.SellOrderLowestPrice, x.Currency),
                })
            );
        }

        /// <summary>
        /// Get the chespest method of aquiring crafting components, sorted by lowest cost
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("market/cheapestCraftingResourceCosts")]
        [ProducesResponseType(typeof(PaginatedResult<MarketCraftingItemCostAnalyticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCraftingCheapestComponentCosts([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var includeFees = this.User.Preference(_db, x => x.ItemIncludeMarketFees);
            var appId = this.App().Guid;
            var query = _db.SteamAssetDescriptions
                .Include(x => x.App)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .Where(x => x.AppId == appId)
                .Where(x => x.IsCraftingComponent)
                .Where(x => x.MarketItem != null)
                .Select(x => new
                {
                    Resource = x,
                    CheapestItem = x.App.AssetDescriptions
                        .Where(y => y.IsBreakable && y.BreaksIntoComponents.Serialised.Contains(x.Name))
                        .Where(y => y.MarketItem != null && y.MarketItem.SellOrderLowestPrice > 0)
                        .OrderBy(y => y.MarketItem.SellOrderLowestPrice)
                        .Select(y => new
                        {
                            Item = y,
                            Currency = y.MarketItem.Currency,
                            BuyNowPrice = y.MarketItem.SellOrderLowestPrice,
                        })
                        .FirstOrDefault()
                })
                .OrderBy(x => x.Resource.MarketItem.SellOrderLowestPrice);

            return Ok(
                query.Paginate(start, count, x => new MarketCraftingItemCostAnalyticDTO
                {
                    Id = x.Resource.ClassId ?? 0,
                    AppId = ulong.Parse(x.Resource.App.SteamId),
                    Name = x.Resource.Name,
                    BackgroundColour = x.Resource.BackgroundColour,
                    ForegroundColour = x.Resource.ForegroundColour,
                    IconUrl = x.Resource.IconUrl,
                    BuyFrom = x.Resource.MarketItem.BuyNowFrom,
                    BuyPrice = this.Currency().CalculateExchange(x.Resource.MarketItem.BuyNowPrice, x.Resource.MarketItem.Currency),
                    BuyFee = (includeFees ? this.Currency().CalculateExchange(x.Resource.MarketItem.BuyNowFee, x.Resource.MarketItem.Currency) : 0),
                    BuyUrl = x.Resource.MarketItem.Description.GetBuyPrices(x.Resource.MarketItem.Currency)?.FirstOrDefault(p => p.MarketType == x.Resource.MarketItem.BuyNowFrom)?.Url,
                    CheapestItem = new ItemValueStatisticDTO()
                    {
                        Id = x.CheapestItem.Item.ClassId ?? 0,
                        AppId = ulong.Parse(x.CheapestItem.Item.App.SteamId),
                        Name = x.CheapestItem.Item.Name,
                        BackgroundColour = x.CheapestItem.Item.BackgroundColour,
                        ForegroundColour = x.CheapestItem.Item.ForegroundColour,
                        IconUrl = x.CheapestItem.Item.IconUrl,
                        BuyNowPrice = this.Currency().CalculateExchange(x.CheapestItem.BuyNowPrice, x.CheapestItem.Currency),
                    }
                })
            );
        }

        /// <summary>
        /// Get the cheapest method of aquiring craftable item containers, sorted by lowest cost
        /// </summary>
        /// <param name="start">Return items starting at this specific index (pagination)</param>
        /// <param name="count">Number items to be returned (can be less if not enough data)</param>
        /// <response code="200">Paginated list of items matching the request parameters.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("market/cheapestCraftableContainerCosts")]
        [ProducesResponseType(typeof(PaginatedResult<MarketCraftableItemCostAnalyticDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMarketCheapestCraftableContainerCosts([FromQuery] int start = 0, [FromQuery] int count = 10)
        {
            var includeFees = this.User.Preference(_db, x => x.ItemIncludeMarketFees);
            var appId = this.App().Guid;
            var resources = await _db.SteamAssetDescriptions
                .Include(x => x.App)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .Where(x => x.AppId == appId)
                .Where(x => x.IsCraftingComponent)
                .Where(x => x.MarketItem != null)
                .Select(x => new
                {
                    Resource = x,
                    ResourcePrice = (x.MarketItem.SellOrderLowestPrice > 0 ? (x.MarketItem.SellOrderLowestPrice / x.MarketItem.Currency.ExchangeRateMultiplier) : 0),
                    CheapestItem = x.App.AssetDescriptions
                        .Where(y => y.IsBreakable && y.BreaksIntoComponents.Serialised.Contains(x.Name))
                        .Where(y => y.MarketItem != null && y.MarketItem.SellOrderLowestPrice > 0)
                        .OrderBy(y => y.MarketItem.SellOrderLowestPrice)
                        .Select(y => new
                        {
                            Item = y,
                            ItemPrice = (y.MarketItem.SellOrderLowestPrice / y.MarketItem.Currency.ExchangeRateMultiplier),
                            MarketItem = y.MarketItem
                        })
                        .FirstOrDefault()
                })
                .OrderBy(x => x.Resource.MarketItem.SellOrderLowestPrice)
                .ToListAsync();

            var query = _db.SteamAssetDescriptions
                .Include(x => x.App)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .Where(x => x.AppId == appId)
                .Where(x => x.IsCraftable)
                .Where(x => x.MarketItem != null)
                .OrderBy(x => x.MarketItem.SellOrderLowestPrice);

            return Ok(
                query.Paginate(start, count, x => new MarketCraftableItemCostAnalyticDTO
                {
                    Id = x.ClassId ?? 0,
                    AppId = ulong.Parse(x.App.SteamId),
                    Name = x.Name,
                    BackgroundColour = x.BackgroundColour,
                    ForegroundColour = x.ForegroundColour,
                    IconUrl = x.IconUrl,
                    BuyFrom = x.MarketItem.BuyNowFrom,
                    BuyPrice = this.Currency().CalculateExchange(x.MarketItem.BuyNowPrice, x.MarketItem.Currency),
                    BuyFee = (includeFees ? this.Currency().CalculateExchange(x.MarketItem.BuyNowFee, x.MarketItem.Currency) : 0),
                    BuyUrl = x.MarketItem.Description.GetBuyPrices(x.MarketItem.Currency)?.FirstOrDefault(p => p.MarketType == x.MarketItem.BuyNowFrom)?.Url,
                    CraftingComponents = x.CraftingComponents
                        .Join(resources, x => x.Key, x => x.Resource.Name, (x, y) => new
                        {
                            Name = y.Resource.Name,
                            Quantity = x.Value,
                            CheapestItem = (y.ResourcePrice <= y.CheapestItem.ItemPrice) ? y.Resource : y.CheapestItem.Item,
                        })
                        .Select(y => new ItemCraftingComponentCostDTO()
                        {
                            Name = y.Name,
                            Quantity = y.Quantity,
                            Component = new ItemValueStatisticDTO
                            {
                                Id = y.CheapestItem.ClassId ?? 0,
                                AppId = ulong.Parse(y.CheapestItem.App.SteamId),
                                Name = y.CheapestItem.Name,
                                BackgroundColour = y.CheapestItem.BackgroundColour,
                                ForegroundColour = y.CheapestItem.ForegroundColour,
                                IconUrl = y.CheapestItem.IconUrl,
                                BuyNowPrice = (y.CheapestItem[this.Currency()]?.Price ?? 0),
                            }
                        })
                })
            );
        }

    }
}
