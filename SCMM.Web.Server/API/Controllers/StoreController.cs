using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Store;
using SCMM.Web.Server.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.API.Controllers
{
    [ApiController]
    [Route("api/store")]
    public class StoreController : ControllerBase
    {
        private readonly ILogger<StoreController> _logger;
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public StoreController(ILogger<StoreController> logger, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        /// <summary>
        /// List all known item store instances
        /// </summary>
        /// <returns>List of stores</returns>
        /// <response code="200">List of known item stores. Use <see cref="GetStore(string)"/> <code>/store/{dateTime}</code> to get the details of a specific store.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<StoreIdentiferDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStores()
        {
            var appId = this.App();
            var itemStores = await _db.SteamItemStores
                .AsNoTracking()
                .Where(x => x.App.SteamId == appId)
                .OrderByDescending(x => x.Start)
                .ToListAsync();

            return Ok(
                itemStores.Select(x => _mapper.Map<SteamItemStore, StoreIdentiferDTO>(x, this)).ToList()
            );
        }

        /// <summary>
        /// Get the most recent item store information
        /// </summary>
        /// <remarks>
        /// There may be multiple active item stores, only the most recent is returned.
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <returns>The most recent item store</returns>
        /// <response code="200">The most recent item store.</response>
        /// <response code="404">If there are no currently active item stores.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("current")]
        [ProducesResponseType(typeof(StoreDetailsDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCurrentStore()
        {
            return await GetStore(DateTime.UtcNow.ToString(Constants.SCMMStoreIdDateFormat));
        }

        /// <summary>
        /// Get item store information
        /// </summary>
        /// <param name="dateTime">UTC date time to load the item store for. Formatted as <code>yyyy-MM-dd-HHmm</code>.</param>
        /// <returns>The item store details</returns>
        /// <remarks>
        /// If there are multiple active stores at the specified date time, only the most recent will be returned.
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <response code="200">The store details.</response>
        /// <response code="400">If the store date is invalid or cannot be parsed as a date time.</response>
        /// <response code="404">If the store cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{dateTime}")]
        [ProducesResponseType(typeof(StoreDetailsDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStore([FromRoute] string dateTime)
        {
            var storeDate = DateTime.UtcNow;
            if (!DateTime.TryParseExact(dateTime, Constants.SCMMStoreIdDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out storeDate))
            {
                if (!DateTime.TryParse(dateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out storeDate))
                {
                    return BadRequest("Store date is invalid and cannot be parsed");
                }
            }

            var itemStore = await _db.SteamItemStores
                .AsNoTracking()
                .OrderByDescending(x => x.Start)
                .Where(x => storeDate >= x.Start)
                .Take(1)
                .Include(x => x.Items).ThenInclude(x => x.Item)
                .Include(x => x.Items).ThenInclude(x => x.Item.App)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.Creator)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.MarketItem)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.MarketItem.Currency)
                .FirstOrDefaultAsync();

            var itemStoreDetail = _mapper.Map<SteamItemStore, StoreDetailsDTO>(itemStore, this);
            if (itemStoreDetail == null)
            {
                return NotFound();
            }

            // Calculate market price ranks
            var storeItems = itemStoreDetail.Items.Where(x => !String.IsNullOrEmpty(x.ItemType));
            if (storeItems.Any())
            {
                var storeItemIds = storeItems.Select(x => x.Id.ToString()).ToArray();
                var marketPriceRanks = await _db.SteamStoreItems
                    .Where(x => storeItemIds.Contains(x.SteamId))
                    .Select(x => new
                    {
                        ItemId = x.SteamId,
                        Position = x.App.MarketItems
                            .Where(y => y.Description.IsMarketable)
                            .Where(y => y.Description.ItemType == x.Description.ItemType)
                            .Where(y => y.BuyNowPrice < ((x.IsAvailable || x.Description.MarketItem == null) ? x.Price ?? 0 : x.Description.MarketItem.BuyNowPrice))
                            .Count(),
                        Total = x.App.MarketItems
                            .Where(y => y.Description.IsMarketable)
                            .Where(y => y.Description.ItemType == x.Description.ItemType)
                            .Count(),
                    })
                    .ToListAsync();

                foreach (var marketPriceRank in marketPriceRanks)
                {
                    var storeItem = storeItems.FirstOrDefault(x => x.Id.ToString() == marketPriceRank.ItemId);
                    if (storeItem != null)
                    {
                        storeItem.MarketRankIndex = marketPriceRank.Position;
                        storeItem.MarketRankTotal = marketPriceRank.Total;
                    }
                }
            }

            return Ok(itemStoreDetail);
        }

        /// <summary>
        /// Get store item sales chart data
        /// </summary>
        /// <param name="id">Store GUID to load item sales for.</param>
        /// <remarks>Item sales data is only available for items that have an associated workshop item.</remarks>
        /// <returns>The item sales chart data</returns>
        /// <response code="200">The item sales chart data.</response>
        /// <response code="400">If the store id is invalid.</response>
        /// <response code="404">If the store cannot be found, or doesn't contain any workshop items.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/stats/itemSales")]
        [ProducesResponseType(typeof(IEnumerable<StoreChartItemSalesDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStoreItemSalesStats([FromRoute] Guid id)
        {
            if (Guid.Empty == id)
            {
                return BadRequest("Store GUID is invalid");
            }

            var storeItems = await _db.SteamItemStores
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Take(1)
                .SelectMany(x => x.Items)
                .Where(x => x.Item != null && x.Item.Description != null)
                .Where(x => x.Item.Description.LifetimeSubscriptions > 0)
                .Select(x => new
                {
                    Name = x.Item.Description.Name,
                    Subscriptions = x.Item.Description.LifetimeSubscriptions ?? 0,
                    TotalSalesMin = x.Item.TotalSalesMin ?? 0,
                    KnownInventoryDuplicates = x.Item.Description.InventoryItems
                        .GroupBy(y => y.ProfileId)
                        .Where(y => y.Count() > 1)
                        .Select(y => y.Sum(z => z.Quantity))
                        .Sum(x => x)
                })
                .OrderBy(x => (x.Subscriptions + x.KnownInventoryDuplicates))
                .ToListAsync();

            if (storeItems?.Any() != true)
            {
                return NotFound("Store not found, or doesn't contain any workshop items");
            }

            var storeItemSales = storeItems.Select(
                x => new StoreChartItemSalesDTO
                {
                    Name = x.Name,
                    Subscriptions = x.Subscriptions,
                    KnownInventoryDuplicates = x.KnownInventoryDuplicates,
                    EstimatedOtherDuplicates = Math.Max(0, x.TotalSalesMin - x.Subscriptions - x.KnownInventoryDuplicates)
                }
            );

            return Ok(storeItemSales);
        }

        /// <summary>
        /// Get store item revenue chart data
        /// </summary>
        /// <param name="id">Store GUID to load item revenue for.</param>
        /// <remarks>
        /// Item revenue data is only available for items that have an associated workshop item.
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// </remarks>
        /// <returns>The item revenue chart data</returns>
        /// <response code="200">The item revenue chart data.</response>
        /// <response code="400">If the store id is invalid.</response>
        /// <response code="404">If the store cannot be found, or doesn't contain any workshop items.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/stats/itemRevenue")]
        [ProducesResponseType(typeof(IEnumerable<StoreChartItemRevenueDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStoreItemRevenueStats([FromRoute] Guid id)
        {
            if (Guid.Empty == id)
            {
                return BadRequest("Store GUID is invalid");
            }

            var storeItems = await _db.SteamItemStores
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Take(1)
                .SelectMany(x => x.Items)
                .Where(x => x.Item != null && x.Item.Description != null)
                .Where(x => x.Item.Description.LifetimeSubscriptions > 0)
                .Select(x => new
                {
                    Name = x.Item.Description.Name,
                    Currency = x.Item.Currency,
                    Price = x.Item.Price ?? 0,
                    Prices = x.Item.Prices,
                    Subscriptions = x.Item.Description.LifetimeSubscriptions ?? 0,
                    KnownInventoryDuplicates = x.Item.Description.InventoryItems
                        .GroupBy(y => y.ProfileId)
                        .Where(y => y.Count() > 1)
                        .Select(y => y.Sum(z => z.Quantity))
                        .Sum(x => x)
                })
                .OrderBy(x => (x.Subscriptions + x.KnownInventoryDuplicates) * x.Price)
                .ToListAsync();

            if (storeItems?.Any() != true)
            {
                return NotFound("Store not found, or doesn't contain any workshop items");
            }

            // NOTE: Steam revenue will always be collected in USD. Convert to the user local currency after calculating the final result
            var steamCurrency = await _db.SteamCurrencies.FirstOrDefaultAsync(x => x.Name == Constants.SteamCurrencyUSD);
            var storeItemRevenue = (
                from storeItem in storeItems
                let total = ((storeItem.Subscriptions + storeItem.KnownInventoryDuplicates) * (storeItem.Prices.ContainsKey(steamCurrency.Name) ? storeItem.Prices[steamCurrency.Name] : 0))
                let salesTax = (long) Math.Round(total * 0.20)
                let totalAfterTax = (total - salesTax)
                let authorRevenue = EconomyExtensions.SteamFeeAuthorComponentAsInt(totalAfterTax)
                let platformRevenue = 0 // TODO: EconomyExtensions.SteamFeePlatformComponentAsInt(total - authorRoyalties)
                let publisherRevenue = (totalAfterTax - platformRevenue - authorRevenue)
                select new StoreChartItemRevenueDTO
                {
                    Name = storeItem.Name,
                    SalesTax = this.Currency().ToPrice(this.Currency().CalculateExchange(salesTax, steamCurrency)),
                    AuthorRevenue = this.Currency().ToPrice(this.Currency().CalculateExchange(authorRevenue, steamCurrency)),
                    PlatformRevenue = this.Currency().ToPrice(this.Currency().CalculateExchange(platformRevenue, steamCurrency)),
                    PublisherRevenue = this.Currency().ToPrice(this.Currency().CalculateExchange(publisherRevenue, steamCurrency))
                }
            );

            return Ok(storeItemRevenue);
        }

        /// <summary>
        /// Link an item to a store
        /// </summary>
        /// <remarks>This API requires authentication and the user must belong to the <code>Moderator</code> role</remarks>
        /// <param name="id">Store GUID to link the item to.</param>
        /// <param name="command">
        /// The item ID and store price (in USD) of the item to be linked to the store
        /// </param>
        /// <response code="200">If the item was linked successfully.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="401">If the request is unauthenticated (login first) or the authenticated user is not a moderator.</response>
        /// <response code="404">If the store or item cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(Roles = Roles.Moderator)]
        [HttpPost("{id}/linkItem")]
        [ProducesResponseType(typeof(SteamStoreItemItemStore), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LinkStoreItem([FromRoute] Guid id, [FromBody] LinkStoreItemCommand command)
        {
            if (Guid.Empty == id)
            {
                return BadRequest("Store GUID is invalid");
            }
            if (command == null)
            {
                return BadRequest("Command is invalid");
            }

            var store = await _db.SteamItemStores
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (store == null)
            {
                return NotFound("Store not found");
            }

            var assetDescription = await _db.SteamAssetDescriptions
                .Include(x => x.App)
                .Include(x => x.Creator)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .FirstOrDefaultAsync(x => x.ClassId == command.AssetDescriptionId);
            if (assetDescription == null)
            {
                return NotFound("Asset description not found");
            }

            var storeItemLink = (SteamStoreItemItemStore)null;
            var storeItem = assetDescription?.StoreItem;
            if (storeItem == null)
            {
                _db.SteamStoreItems.Add(
                    storeItem = assetDescription.StoreItem = new SteamStoreItem()
                    {
                        App = assetDescription.App,
                        Description = assetDescription
                    }
                );
            }

            if (!storeItem.PricesAreLocked && command.StorePrice > 0)
            {
                // NOTE: This assumes the input price is supplied in USD
                var currencies = await _db.SteamCurrencies.ToListAsync();
                var storeCurrency = currencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
                storeItem.Currency = storeCurrency;
                storeItem.Price = command.StorePrice;
                foreach (var currency in currencies)
                {
                    var exchangeRate = await _db.SteamCurrencyExchangeRates
                        .Where(x => x.CurrencyId == currency.Name)
                        .Where(x => x.Timestamp > store.Start)
                        .OrderBy(x => x.Timestamp)
                        .Take(1)
                        .Select(x => x.ExchangeRateMultiplier)
                        .FirstOrDefaultAsync();

                    storeItem.Prices[currency.Name] = EconomyExtensions.SteamStorePriceRounded(
                        exchangeRate.CalculateExchange(command.StorePrice)
                    );
                }
            }

            if (!store.Items.Any(x => x.ItemId == storeItem.Id))
            {
                store.Items.Add(storeItemLink = new SteamStoreItemItemStore()
                {
                    Store = store,
                    Item = assetDescription.StoreItem,
                    IsDraft = true
                });
            }

            await _db.SaveChangesAsync();
            return Ok(
                _mapper.Map<SteamStoreItemItemStore, StoreItemDetailsDTO>(storeItemLink, this)
            );
        }

        /// <summary>
        /// Unlink an item from a store
        /// </summary>
        /// <remarks>This API requires authentication and the user must belong to the <code>Moderator</code> role</remarks>
        /// <param name="id">Store GUID to unlink the item from.</param>
        /// <param name="command">
        /// The item ID and store price (in USD) of the item to be unlinked from the store
        /// </param>
        /// <response code="200">If the item was unlinked successfully.</response>
        /// <response code="400">If the request data is malformed/invalid.</response>
        /// <response code="401">If the request is unauthenticated (login first) or the authenticated user is not a moderator.</response>
        /// <response code="404">If the store or item cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(Roles = Roles.Moderator)]
        [HttpPost("{id}/unlinkItem")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UnlinkStoreItem([FromRoute] Guid id, [FromBody] UnlinkStoreItemCommand command)
        {
            if (Guid.Empty == id)
            {
                return BadRequest("Store GUID is invalid");
            }
            if (command == null)
            {
                return BadRequest("Command is invalid");
            }

            var store = await _db.SteamItemStores
                .Include(x => x.Items).ThenInclude(x => x.Item).ThenInclude(x => x.Description)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (store == null)
            {
                return NotFound("Store not found");
            }

            var storeItemLink = store.Items.FirstOrDefault(x => x.Item.Description.ClassId == command.AssetDescriptionId);
            if (storeItemLink == null)
            {
                return NotFound("Asset description not found in store");
            }

            store.Items.Remove(storeItemLink);
            await _db.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Get the next (estimated) item store release date time
        /// </summary>
        /// <remarks>This is an estimate only and the exact time varies from week to week. Sometimes the store can even be late by a day or two.</remarks>
        /// <returns>The expected store release date/time</returns>
        /// <response code="200">The expected date/time of the next item store release.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("nextUpdateTime")]
        [ProducesResponseType(typeof(DateTimeOffset?), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStoreNextUpdateTime()
        {
            var nextUpdateTime = await _queryProcessor.ProcessAsync(new GetStoreNextUpdateTimeRequest());
            return Ok(nextUpdateTime?.Timestamp);
        }
    }
}
