using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Blob;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SCMM.Steam.API.Commands
{
    public class ImportSteamItemStorePricesRequest : ICommand
    {
        public string ItemStoreUrl { get; set; }

        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    }

    public class ImportSteamItemStorePrices : ICommandHandler<ImportSteamItemStorePricesRequest>
    {
        private readonly ILogger<ImportSteamItemStorePrices> _logger;
        private readonly SteamDbContext _db;
        private readonly SteamCommunityWebClient _client;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public ImportSteamItemStorePrices(ILogger<ImportSteamItemStorePrices> logger, SteamDbContext db, SteamCommunityWebClient client, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _logger = logger;
            _db = db;
            _client = client;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        public async Task HandleAsync(ImportSteamItemStorePricesRequest request)
        {
            var currencies = await _db.SteamCurrencies.ToListAsync();
            var storePageHtml = await _client.GetHtmlAsync(new SteamBlobRequest(request.ItemStoreUrl));
            if (storePageHtml == null)
            {
                throw new Exception("Unable to load store page");
            }

            // Find the most appropriate store
            var store = await _db.SteamItemStores
                .Where(x => x.Start != null && request.Timestamp >= x.Start)
                .OrderByDescending(x => x.Start)
                .Take(1)
                .Include(x => x.Items)
                .FirstOrDefaultAsync();

            // Find all item definitions in the page ("top sellers" and "new releases")
            var itemGridDefinitions = storePageHtml.Descendants("div").Where(x => x?.Attribute("class")?.Value == "item_def_grid_item").ToList();
            var itemRowDefinitions = storePageHtml.Descendants("div").Where(x => x?.Attribute("class")?.Value?.Contains("item_def_row_item") == true).ToList();
            var itemAllDefinitions = itemGridDefinitions.Union(itemRowDefinitions);
            foreach (var itemDefinition in itemAllDefinitions)
            {
                // Find the correct "name", "price" and "url" elements
                var itemDefinitionPrice = itemDefinition.Descendants().FirstOrDefault(x => x?.Attribute("class")?.Value?.Contains("item_def_price") == true);
                var itemDefinitionName = itemDefinition.Descendants().FirstOrDefault(x => x?.Attribute("class")?.Value?.Contains("item_def_name") == true);
                var itemDefinitionDetailsLink = (XElement)null;
                if (itemDefinitionName.Descendants("a")?.Any() == true)
                {
                    itemDefinitionDetailsLink = itemDefinition.Descendants().FirstOrDefault(x => x?.Attribute("class")?.Value?.Contains("item_def_name") == true)?.Descendants("a")?.FirstOrDefault();
                    itemDefinitionName = itemDefinitionDetailsLink;
                }
                else
                {
                    itemDefinitionDetailsLink = itemDefinition?.Parent;
                }

                // Create the store item for this item definition (if missing)
                var itemName = itemDefinitionName?.Value;
                var storeItem = _db.SteamStoreItems
                    .Include(x => x.Stores).ThenInclude(x => x.Store)
                    .FirstOrDefault(x => x.Description.Name == itemName);
                if (storeItem == null)
                {
                    storeItem = _db.SteamStoreItems.Local.FirstOrDefault(x => x.Description?.Name == itemName);
                }
                if (storeItem == null)
                {
                    var assetDescription = _db.SteamAssetDescriptions
                        .Include(x => x.App)
                        .FirstOrDefault(x => x.Name == itemName);
                    if (assetDescription == null)
                    {
                        continue;
                    }
                    else
                    {
                        _db.SteamStoreItems.Add(
                            storeItem = new SteamStoreItem()
                            {
                                App = assetDescription.App,
                                Description = assetDescription
                            }
                        );
                    }
                }

                // Create the store item link (if missing)
                var storeItemLink = storeItem.Stores.FirstOrDefault(x => x.Store == store);
                if (storeItemLink == null && store != null)
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

                // Parse and update the store item id
                var itemDetailsUrl = itemDefinitionDetailsLink?.Attribute("href")?.Value;
                if (string.IsNullOrEmpty(storeItem.SteamId) && !string.IsNullOrEmpty(itemDetailsUrl))
                {
                    var steamIdMatchGroup = Regex.Match(itemDetailsUrl, Constants.SteamStoreItemDefLinkRegex).Groups;
                    storeItem.SteamId = (steamIdMatchGroup.Count > 1)
                        ? steamIdMatchGroup[1].Value.Trim()
                        : null;
                }

                // Parse and update the store item prices
                var itemPriceText = itemDefinitionPrice?.Value;
                if (!string.IsNullOrEmpty(itemPriceText))
                {
                    // NOTE: Unless specified in the prefix/suffix text, the price is assumed to be USD
                    var usdCurrency = currencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
                    var mostLikelyCurrencies = currencies
                        .Where(x =>
                            (!string.IsNullOrEmpty(x.PrefixText) && itemPriceText.Contains(x.PrefixText)) ||
                            (!string.IsNullOrEmpty(x.SuffixText) && itemPriceText.Contains(x.SuffixText))
                        )
                        .OrderBy(x => int.Parse(x.SteamId));

                    var itemPriceCurrency = (mostLikelyCurrencies.FirstOrDefault() ?? usdCurrency);
                    var itemPrice = itemPriceText.SteamPriceAsInt();
                    if (itemPrice > 0)
                    {
                        var itemPrices = new PersistablePriceDictionary();
                        foreach (var currency in currencies)
                        {
                            var exchangeRate = await _db.SteamCurrencyExchangeRates
                                .Where(x => x.CurrencyId == currency.Name)
                                .Where(x => x.Timestamp > request.Timestamp)
                                .OrderBy(x => x.Timestamp)
                                .Take(1)
                                .Select(x => x.ExchangeRateMultiplier)
                                .FirstOrDefaultAsync();

                            itemPrices[currency.Name] = EconomyExtensions.SteamPriceRounded(
                                exchangeRate.CalculateExchange(itemPrice)
                            );
                        }

                        if (storeItemLink != null)
                        {
                            storeItemLink.Currency = itemPriceCurrency;
                            storeItemLink.Price = itemPrice;
                            storeItemLink.Prices = new PersistablePriceDictionary(itemPrices);
                            storeItemLink.IsPriceVerified = true;
                            storeItem.UpdateLatestPrice();
                        }
                        else
                        {
                            storeItem.UpdatePrice(itemPriceCurrency, itemPrice, itemPrices);
                        }
                    }
                }
            }
        }
    }
}
