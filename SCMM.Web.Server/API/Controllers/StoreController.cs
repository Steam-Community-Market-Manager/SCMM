using AutoMapper;
using CommandQuery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.API.Queries;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using SCMM.Web.Data.Models.UI.Store;
using SCMM.Web.Server.Extensions;
using System.Globalization;

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
        /// <remarks>Response is cached for 7 days.</remarks>
        /// <response code="200">List of known item stores. Use <see cref="GetStore(string)"/> <code>/store/{dateTime}</code> to get the details of a specific store.</response>
        /// <response code="400">If the current app does not support stores.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<StoreIdentifierDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OutputCache(PolicyName = CachePolicy.Expire7d, Tags = [CacheTag.Store])]
        public async Task<IActionResult> GetStores()
        {
            var app = this.App();
            if (app?.FeatureFlags.HasFlag(SteamAppFeatureFlags.ItemStore) != true)
            {
                return BadRequest("App does not support stores");
            }

            if (app?.FeatureFlags.HasFlag(SteamAppFeatureFlags.ItemStoreRotating) == true)
            {
                var itemStores = await _db.SteamItemStores
                    .AsNoTracking()
                    .Where(x => x.AppId == app.Guid)
                    .OrderByDescending(x => x.Start == null)
                    .ThenByDescending(x => x.Start)
                    .ToListAsync();

                return Ok(
                    itemStores
                        .Select(x => _mapper.Map<SteamItemStore, StoreIdentifierDTO>(x, this))
                        .ToArray()
                );
            }
            else
            {
                return Ok(new StoreIdentifierDTO[0]);
            }

        }

        /// <summary>
        /// Get the most recent item store information
        /// </summary>
        /// <remarks>
        /// There may be multiple active item stores, only the most recent is returned.
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// Response is cached for 10 minutes.
        /// </remarks>
        /// <returns>The most recent item store</returns>
        /// <response code="200">The most recent item store.</response>
        /// <response code="400">If the current app does not support stores.</response>
        /// <response code="404">If there are no currently active item stores.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("current")]
        [ProducesResponseType(typeof(StoreDetailsDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OutputCache(PolicyName = CachePolicy.Expire10m, Tags = [CacheTag.Store])]
        public async Task<IActionResult> GetCurrentStore()
        {
            return await GetStore(DateTime.UtcNow.ToString(Constants.SCMMStoreIdDateFormat));
        }

        /// <summary>
        /// Get item store information at a specific date
        /// </summary>
        /// <param name="id">Store GUID, Name, or UTC start date time (formatted as <code>yyyy-MM-dd-HHmm</code>).</param>
        /// <returns>The item store details</returns>
        /// <remarks>
        /// If there are multiple active stores at the specified date time, only the most recent will be returned.
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// Response is cached for 1 hour.
        /// </remarks>
        /// <response code="200">The store details.</response>
        /// <response code="400">If the store date is invalid or cannot be parsed as a date time or the current app does not support stores.</response>
        /// <response code="404">If the store cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(StoreDetailsDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OutputCache(PolicyName = CachePolicy.Expire1h, Tags = [CacheTag.Store])]
        public async Task<IActionResult> GetStore([FromRoute] string id)
        {
            var app = this.App();
            if (app?.FeatureFlags.HasFlag(SteamAppFeatureFlags.ItemStore) != true)
            {
                return BadRequest("App does not support stores");
            }

            var guid = Guid.Empty;
            var storeStartDate = DateTime.MinValue;
            var storeName = (string)null;
            if (!Guid.TryParse(id, out guid))
            {
                if (!DateTime.TryParseExact(id, Constants.SCMMStoreIdDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out storeStartDate))
                {
                    if (!DateTime.TryParse(id, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out storeStartDate))
                    {
                        storeName = id;
                    }
                }
            }

            var itemStore = await _db.SteamItemStores
                .AsNoTracking()
                .OrderByDescending(x => x.Start)
                .Where(x => x.AppId == app.Guid)
                .Where(x => (guid != Guid.Empty && guid == x.Id) || (storeStartDate > DateTime.MinValue && x.Start != null && storeStartDate >= x.Start) || (!String.IsNullOrEmpty(storeName) && storeName == x.Name) || x.Start == null)
                .Take(1)
                .Include(x => x.Items).ThenInclude(x => x.Item)
                .Include(x => x.Items).ThenInclude(x => x.Item.App)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.CreatorProfile)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.MarketItem)
                .Include(x => x.Items).ThenInclude(x => x.Item.Description.MarketItem.Currency)
                .FirstOrDefaultAsync();

            var itemStoreDetail = _mapper.Map<SteamItemStore, StoreDetailsDTO>(itemStore, this);
            if (itemStoreDetail == null)
            {
                return NotFound();
            }

            // Calculate market price ranks
            var storeItems = itemStoreDetail.Items.Where(x => !string.IsNullOrEmpty(x.ItemType));
            if (storeItems.Any())
            {
                var allItemTypes = storeItems.Select(x => x.ItemType).Distinct();
                var allItemPrices = await _db.SteamAssetDescriptions
                    .AsNoTracking()
                    .Where(x => allItemTypes.Contains(x.ItemType))
                    .Where(x => x.IsMarketable || x.MarketableRestrictionDays > 0)
                    .Select(x => new
                    {
                        ItemType = x.ItemType,
                        MarketPrice = (x.MarketItem != null ? x.MarketItem.SellOrderLowestPrice : 0),
                        MarketCurrency = (x.MarketItem != null ? x.MarketItem.Currency : null),
                        StorePrices = (x.StoreItem != null && x.StoreItem.IsAvailable ? x.StoreItem.Prices : null)
                    })
                    .ToListAsync();

                foreach (var storeItem in storeItems)
                {
                    var cheapestPrice = (storeItem.IsStillAvailableFromStore
                        ? (storeItem.StorePrice > 0 && storeItem.MarketPrice > 0) ? Math.Min(storeItem.StorePrice.Value, storeItem.MarketPrice.Value) : storeItem.StorePrice
                        : storeItem.MarketPrice
                    );
                    var allItems = allItemPrices.Where(x => x.ItemType == storeItem.ItemType).ToList();
                    var cheaperItems = allItems.Where(x =>
                        (x.MarketPrice > 0 && this.Currency().CalculateExchange(x.MarketPrice, x.MarketCurrency) < cheapestPrice) ||
                        (x.StorePrices != null && x.StorePrices.ContainsKey(this.Currency().Name) && x.StorePrices[this.Currency().Name] < cheapestPrice)
                    );
                    storeItem.MarketRankIndex = cheaperItems.Count();
                    storeItem.MarketRankTotal = allItems.Count();
                }
            }

            // Sort items based on users preference (if any)
            var topSellersRanking = this.User.Preference(_db, x => x.StoreTopSellers);
            switch (topSellersRanking)
            {
                case StoreTopSellerRankingType.SteamStoreRanking:
                    itemStoreDetail.Items = itemStoreDetail.Items
                        .OrderByDescending(x => x.TopSellerIndex != null)
                        .ThenBy(x => x.TopSellerIndex)
                        .ThenByDescending(x => (x.SupplyTotalEstimated ?? 0) * x.StorePrice)
                        .ThenByDescending(x => (x.Subscriptions ?? 0) * x.StorePrice)
                        .ToArray();
                    break;
                case StoreTopSellerRankingType.HighestTotalRevenue:
                    itemStoreDetail.Items = itemStoreDetail.Items
                        .OrderByDescending(x => (x.SupplyTotalEstimated ?? 0) * x.StorePrice)
                        .ThenByDescending(x => (x.Subscriptions ?? 0) * x.StorePrice)
                        .ToArray();
                    break;
                case StoreTopSellerRankingType.HighestTotalSales:
                    itemStoreDetail.Items = itemStoreDetail.Items
                        .OrderByDescending(x => (x.SupplyTotalEstimated ?? 0))
                        .ThenByDescending(x => (x.Subscriptions ?? 0))
                        .ToArray();
                    break;
            }

            return Ok(itemStoreDetail);
        }

        /// <summary>
        /// Get store item sales chart data
        /// </summary>
        /// <param name="id">Store GUID to load item sales for.</param>
        /// <remarks>
        /// Item sales data is only available for items that have an associated workshop item.
        /// Response is cached for 1 hour.
        /// </remarks>
        /// <returns>The item sales chart data</returns>
        /// <response code="200">The item sales chart data.</response>
        /// <response code="400">If the store id is invalid or the current app does not support stores.</response>
        /// <response code="404">If the store cannot be found, or doesn't contain any workshop items.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/stats/itemSales")]
        [ProducesResponseType(typeof(IEnumerable<StoreChartItemSalesDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OutputCache(PolicyName = CachePolicy.Expire1h, Tags = [CacheTag.Store])]
        public async Task<IActionResult> GetStoreItemSalesStats([FromRoute] Guid id)
        {
            var app = this.App();
            if (app?.FeatureFlags.HasFlag(SteamAppFeatureFlags.ItemStoreRotating) != true)
            {
                return BadRequest("App does not support store sale statistics");
            }

            if (Guid.Empty == id)
            {
                return BadRequest("Store GUID is invalid");
            }

            var storeItems = await _db.SteamItemStores
                .AsNoTracking()
                .Where(x => x.AppId == app.Guid)
                .Where(x => x.Id == id)
                .Take(1)
                .SelectMany(x => x.Items)
                .Where(x => x.Item != null && x.Item.Description != null)
                .Where(x => x.Item.Description.SupplyTotalEstimated > 0 || x.Item.Description.SubscriptionsLifetime > 0)
                .Select(x => new
                {
                    Name = x.Item.Description.Name,
                    SupplyTotalEstimated = x.Item.Description.SupplyTotalEstimated ?? x.Item.Description.SubscriptionsLifetime ?? 0,
                    SupplyTotalMarketsKnown = x.Item.Description.SupplyTotalMarketsKnown ?? 0,
                    SupplyTotalInvestorsKnown = x.Item.Description.SupplyTotalInvestorsKnown ?? 0,
                    SupplyTotalInvestorsEstimated = x.Item.Description.SupplyTotalInvestorsEstimated ?? 0,
                    SupplyTotalOwnersKnown = x.Item.Description.SupplyTotalOwnersKnown ?? 0,
                    SupplyTotalOwnersEstimated = x.Item.Description.SupplyTotalOwnersEstimated ?? 0,
                })
                .OrderBy(x => x.SupplyTotalEstimated)
                .ToListAsync();

            if (storeItems?.Any() != true)
            {
                return NotFound("Store not found, or doesn't contain any workshop items");
            }

            var storeItemSales = storeItems.Select(
                x => new StoreChartItemSalesDTO
                {
                    Name = x.Name,
                    SupplyTotalEstimated = x.SupplyTotalEstimated,
                    SupplyTotalMarketsKnown = x.SupplyTotalMarketsKnown,
                    SupplyTotalInvestorsKnown = x.SupplyTotalInvestorsKnown,
                    SupplyTotalInvestorsEstimated = Math.Max(x.SupplyTotalInvestorsEstimated - x.SupplyTotalInvestorsKnown, 0),
                    SupplyTotalOwnersKnown = x.SupplyTotalOwnersKnown,
                    SupplyTotalOwnersEstimated = Math.Max(x.SupplyTotalOwnersEstimated - x.SupplyTotalOwnersKnown, 0),
                }
            );

            return Ok(storeItemSales.ToArray());
        }

        /// <summary>
        /// Get store item revenue chart data
        /// </summary>
        /// <param name="id">Store GUID to load item revenue for.</param>
        /// <remarks>
        /// Item revenue data is only available for items that have an associated workshop item.
        /// The currency used to represent monetary values can be changed by defining <code>Currency</code> in the request headers or query string and setting it to a supported three letter ISO 4217 currency code (e.g. 'USD').
        /// Response is cached for 1 hour.
        /// </remarks>
        /// <returns>The item revenue chart data</returns>
        /// <response code="200">The item revenue chart data.</response>
        /// <response code="400">If the store id is invalid or the current app does not support stores.</response>
        /// <response code="404">If the store cannot be found, or doesn't contain any workshop items.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("{id}/stats/itemRevenue")]
        [ProducesResponseType(typeof(IEnumerable<StoreChartItemRevenueDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [OutputCache(PolicyName = CachePolicy.Expire1h, Tags = [CacheTag.Store])]
        public async Task<IActionResult> GetStoreItemRevenueStats([FromRoute] Guid id)
        {
            var app = this.App();
            if (app?.FeatureFlags.HasFlag(SteamAppFeatureFlags.ItemStoreRotating) != true)
            {
                return BadRequest("App does not support store revenue statistics");
            }

            if (Guid.Empty == id)
            {
                return BadRequest("Store GUID is invalid");
            }

            var storeItems = await _db.SteamItemStores
                .AsNoTracking()
                .Where(x => x.AppId == app.Guid)
                .Where(x => x.Id == id)
                .Take(1)
                .SelectMany(x => x.Items)
                .Where(x => x.Item != null && x.Item.Description != null)
                .Where(x => x.Item.Description.SupplyTotalEstimated > 0 || x.Item.Description.SubscriptionsLifetime > 0)
                .Select(x => new
                {
                    Name = x.Item.Description.Name,
                    Currency = x.Item.Currency,
                    Price = x.Item.Price,
                    Prices = x.Item.Prices,
                    SupplyTotalEstimated = x.Item.Description.SupplyTotalEstimated ?? x.Item.Description.SubscriptionsLifetime ?? 0,
                })
                .OrderBy(x => x.SupplyTotalEstimated * x.Price)
                .ToListAsync();

            if (storeItems?.Any() != true)
            {
                return NotFound("Store not found, or doesn't contain any workshop items");
            }

            // NOTE: Steam revenue will always be collected in USD. Convert to the user local currency after calculating the final result
            var usdCurrency = await _db.SteamCurrencies.FirstOrDefaultAsync(x => x.Name == Constants.SteamCurrencyUSD);
            var storeItemRevenue = (
                from storeItem in storeItems
                let total = (storeItem.SupplyTotalEstimated * (storeItem.Prices.ContainsKey(usdCurrency.Name) ? storeItem.Prices[usdCurrency.Name] : 0))
                let salesTax = EconomyExtensions.SteamSaleTaxComponentAsInt(total)
                let totalAfterTax = (total - salesTax)
                let authorRevenue = EconomyExtensions.SteamSaleAuthorComponentAsInt(totalAfterTax)
                let totalAfterTaxAndAuthorSplit = (totalAfterTax - authorRevenue)
                let platformRevenue = EconomyExtensions.SteamSalePlatformFeeComponentAsInt(totalAfterTax - authorRevenue)
                let totalAfterTaxAndAuthorAndPlatformSplit = (totalAfterTaxAndAuthorSplit - platformRevenue)
                let publisherRevenue = (totalAfterTax - authorRevenue - platformRevenue)
                select new StoreChartItemRevenueDTO
                {
                    Name = storeItem.Name,
                    SalesTax = this.Currency().ToPrice(this.Currency().CalculateExchange(salesTax, usdCurrency)),
                    AuthorRevenue = this.Currency().ToPrice(this.Currency().CalculateExchange(authorRevenue, usdCurrency)),
                    PlatformRevenue = this.Currency().ToPrice(this.Currency().CalculateExchange(platformRevenue, usdCurrency)),
                    PublisherRevenue = this.Currency().ToPrice(this.Currency().CalculateExchange(publisherRevenue, usdCurrency))
                }
            );

            return Ok(storeItemRevenue.ToArray());
        }

        /// <summary>
        /// Link an item to a store
        /// </summary>
        /// <remarks>This API requires authentication and the user must belong to the <code>Contributor</code> role</remarks>
        /// <param name="id">Store GUID to link the item to.</param>
        /// <param name="command">
        /// The item ID and store price (in USD) of the item to be linked to the store
        /// </param>
        /// <response code="200">If the item was linked successfully.</response>
        /// <response code="400">If the request data is malformed/invalid or the current app does not support stores.</response>
        /// <response code="401">If the request is unauthenticated (login first) or the authenticated user is not a moderator.</response>
        /// <response code="404">If the store or item cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(Roles = $"{Roles.Administrator},{Roles.Contributor}")]
        [HttpPost("{id}/linkItem")]
        [ProducesResponseType(typeof(StoreItemDetailsDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LinkStoreItem([FromRoute] Guid id, [FromBody] LinkStoreItemCommand command)
        {
            var app = this.App();
            if (app?.FeatureFlags.HasFlag(SteamAppFeatureFlags.ItemStorePersistent) != true && app?.FeatureFlags.HasFlag(SteamAppFeatureFlags.ItemStoreRotating) != true)
            {
                return BadRequest("App does not support store item linking");
            }

            if (Guid.Empty == id)
            {
                return BadRequest("Store GUID is invalid");
            }

            var store = await _db.SteamItemStores
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.AppId == app.Guid && x.Id == id);
            if (store == null)
            {
                return NotFound("Store not found");
            }
            if (!store.IsDraft && !this.User.IsInRole(Roles.Administrator))
            {
                return Unauthorized("Store is not a draft and cannot be modified");
            }

            var assetDescriptionId = command?.AssetDescriptionId ?? 0;
            var assetDescription = await _db.SteamAssetDescriptions
                .Include(x => x.App)
                .Include(x => x.CreatorProfile)
                .Include(x => x.MarketItem).ThenInclude(x => x.Currency)
                .Include(x => x.StoreItem).ThenInclude(x => x.Currency)
                .Include(x => x.StoreItem).ThenInclude(x => x.Stores).ThenInclude(x => x.Store)
                .FirstOrDefaultAsync(x => x.AppId == app.Guid && x.ClassId == assetDescriptionId);
            if (assetDescription == null)
            {
                return NotFound("Asset description not found");
            }

            // Create the store item (if missing)
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

            // Create the store item link (if missing)
            var storeItemLink = storeItem.Stores.FirstOrDefault(x => x.StoreId == store.Id);
            if (storeItemLink == null)
            {
                storeItem.Stores.Add(
                    storeItemLink = new SteamStoreItemItemStore()
                    {
                        Store = store,
                        Item = storeItem,
                        IsDraft = true
                    }
                );
            }

            // Set the store item link price
            var storePrice = command?.StorePrice ?? 0;
            if (storePrice > 0 && store.Start != null)
            {
                // Use the user supplied store price info
                // NOTE: This assumes the input price is supplied in USD
                var currencies = await _db.SteamCurrencies.ToListAsync();
                var usdCurrency = currencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
                storeItemLink.Currency = usdCurrency;
                storeItemLink.Price = storePrice;
                foreach (var currency in currencies)
                {
                    var exchangeRate = await _db.SteamCurrencyExchangeRates
                        .Where(x => x.CurrencyId == currency.Name)
                        .Where(x => x.Timestamp > store.Start.Value)
                        .OrderBy(x => x.Timestamp)
                        .Take(1)
                        .Select(x => x.ExchangeRateMultiplier)
                        .FirstOrDefaultAsync();

                    storeItemLink.Prices[currency.Name] = EconomyExtensions.SteamPriceRounded(
                        exchangeRate.CalculateExchange(storePrice)
                    );
                }

                // Update the store items "latest price"
                storeItem.UpdateLatestPrice();
            }
            else
            {
                // Copy the store price from previous store instances (if any)
                storeItemLink.Currency = storeItem.Currency;
                storeItemLink.Price = storeItem.Price;
                storeItemLink.Prices = new PersistablePriceDictionary(storeItem.Prices);
            }

            await _db.SaveChangesAsync();
            return Ok(
                _mapper.Map<SteamStoreItemItemStore, StoreItemDetailsDTO>(storeItemLink, this)
            );
        }

        /// <summary>
        /// Unlink an item from a store
        /// </summary>
        /// <remarks>This API requires authentication and the user must belong to the <code>Contributor</code> role</remarks>
        /// <param name="id">Store GUID to unlink the item from.</param>
        /// <param name="command">
        /// The item ID and store price (in USD) of the item to be unlinked from the store
        /// </param>
        /// <response code="200">If the item was unlinked successfully.</response>
        /// <response code="400">If the request data is malformed/invalid or the current app does not support stores.</response>
        /// <response code="401">If the request is unauthenticated (login first) or the authenticated user is not a moderator.</response>
        /// <response code="404">If the store or item cannot be found.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [Authorize(Roles = $"{Roles.Administrator},{Roles.Contributor}")]
        [HttpPost("{id}/unlinkItem")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UnlinkStoreItem([FromRoute] Guid id, [FromBody] UnlinkStoreItemCommand command)
        {
            var app = this.App();
            if (app?.FeatureFlags.HasFlag(SteamAppFeatureFlags.ItemStorePersistent) != true && app?.FeatureFlags.HasFlag(SteamAppFeatureFlags.ItemStoreRotating) != true)
            {
                return BadRequest("App does not support store item unlinking");
            }

            if (Guid.Empty == id)
            {
                return BadRequest("Store GUID is invalid");
            }

            var store = await _db.SteamItemStores
                .Include(x => x.Items).ThenInclude(x => x.Item).ThenInclude(x => x.Stores).ThenInclude(x => x.Store)
                .Include(x => x.Items).ThenInclude(x => x.Item).ThenInclude(x => x.Description)
                .FirstOrDefaultAsync(x => x.AppId == app.Guid && x.Id == id);
            if (store == null)
            {
                return NotFound("Store not found");
            }
            if (!store.IsDraft && !this.User.IsInRole(Roles.Administrator))
            {
                return Unauthorized("Store is not a draft and cannot be modified");
            }

            var assetDescriptionId = command?.AssetDescriptionId ?? 0;
            var storeItemLink = store.Items.FirstOrDefault(x => x.Item.Description.ClassId == assetDescriptionId);
            if (storeItemLink == null)
            {
                return NotFound("Asset description not found in store");
            }

            var storeItem = storeItemLink.Item;
            _db.SteamStoreItemItemStore.Remove(storeItemLink);
            await _db.SaveChangesAsync();

            // Update the store items "latest price"
            storeItem.Stores.Remove(storeItemLink);
            storeItem.UpdateLatestPrice();

            // Remove the store item link
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
        /// <response code="400">If the current app does not support store rotations.</response>
        /// <response code="500">If the server encountered a technical issue completing the request.</response>
        [AllowAnonymous]
        [HttpGet("nextUpdateTime")]
        [ProducesResponseType(typeof(DateTimeOffset?), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStoreNextUpdateTime()
        {
            var app = this.App();
            if (app?.FeatureFlags.HasFlag(SteamAppFeatureFlags.ItemStoreRotating) != true)
            {
                return BadRequest("App does not support store rotations");
            }

            var nextUpdateTime = await _queryProcessor.ProcessAsync(new GetStoreNextUpdateTimeRequest()
            {
                AppId = app.Id
            });

            return Ok(nextUpdateTime?.Timestamp);
        }
    }
}
