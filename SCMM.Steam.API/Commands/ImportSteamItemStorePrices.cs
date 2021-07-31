using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Blob;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
            var storePageHtml = await _client.GetHtml(new SteamBlobRequest(request.ItemStoreUrl));
            if (storePageHtml == null)
            {
                throw new Exception("Unable to load store page");
            }

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

                // Find the associated asset description for this item definition
                var itemName = itemDefinitionName?.Value;
                var storeItem = _db.SteamStoreItems.FirstOrDefault(x => x.Description.Name == itemName) ??
                                _db.SteamStoreItems.Local.FirstOrDefault(x => x.Description?.Name == itemName);
                if (storeItem == null)
                {
                    var assetDescription = _db.SteamAssetDescriptions.Include(x => x.App).FirstOrDefault(x => x.Name == itemName);
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

                // Parse and update the store item id
                var itemDetailsUrl = itemDefinitionDetailsLink?.Attribute("href")?.Value;
                if (String.IsNullOrEmpty(storeItem.SteamId) && !String.IsNullOrEmpty(itemDetailsUrl))
                {
                    var steamIdMatchGroup = Regex.Match(itemDetailsUrl, Constants.SteamStoreItemDefLinkRegex).Groups;
                    storeItem.SteamId = (steamIdMatchGroup.Count > 1)
                        ? steamIdMatchGroup[1].Value.Trim()
                        : null;
                }

                // Parse and update the store item prices
                var itemPriceText = itemDefinitionPrice?.Value;
                if (!storeItem.PricesAreLocked && !String.IsNullOrEmpty(itemPriceText))
                {
                    // NOTE: Unless specified in the prefix/suffix text, the price is assumed to be USD
                    var possibleCurrencies = currencies
                        .Where(x =>
                            (!String.IsNullOrEmpty(x.PrefixText) && itemPriceText.Contains(x.PrefixText)) ||
                            (!String.IsNullOrEmpty(x.SuffixText) && itemPriceText.Contains(x.SuffixText))
                        )
                        .OrderBy(x => Int32.Parse(x.SteamId));

                    var itemPriceCurrency = (
                        possibleCurrencies.FirstOrDefault() ??
                        currencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD)
                    );

                    var itemPrice = itemPriceText.SteamPriceAsInt();
                    if (itemPrice > 0)
                    {
                        //storeItem.PricesAreLocked = true; // we are 100% confident that these are correct
                        storeItem.Currency = itemPriceCurrency;
                        storeItem.Price = itemPrice;
                        foreach (var currency in currencies)
                        {
                            var exchangeRate = await _db.SteamCurrencyExchangeRates
                                .Where(x => x.CurrencyId == currency.Name)
                                .Where(x => x.Timestamp > request.Timestamp)
                                .OrderBy(x => x.Timestamp)
                                .Take(1)
                                .Select(x => x.ExchangeRateMultiplier)
                                .FirstOrDefaultAsync();

                            storeItem.Prices[currency.Name] = EconomyExtensions.SteamStorePriceRounded(
                                exchangeRate.CalculateExchange(itemPrice)
                            );
                        }
                    }
                }
            }
        }
    }
}
