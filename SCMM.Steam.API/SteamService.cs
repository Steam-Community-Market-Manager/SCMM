using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models.Community.Models;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using Steam.Models.SteamEconomy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SCMM.Steam.API
{
    public class SteamService
    {
        private readonly TimeSpan DefaultCachePeriod = TimeSpan.FromHours(6);

        private readonly SteamDbContext _db;
        private readonly SteamConfiguration _cfg;
        private readonly SteamCommunityWebClient _communityClient;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;


        public SteamService(SteamDbContext db, IConfiguration cfg, SteamCommunityWebClient communityClient, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _communityClient = communityClient;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        public Steam.Data.Store.SteamAssetFilter AddOrUpdateAppAssetFilter(SteamApp app, SCMM.Steam.Data.Models.Community.Models.SteamAppFilter filter)
        {
            var existingFilter = app.Filters.FirstOrDefault(x => x.SteamId == filter.Name);
            if (existingFilter != null)
            {
                // Nothing to update
                return existingFilter;
            }

            var newFilter = new Steam.Data.Store.SteamAssetFilter()
            {
                SteamId = filter.Name,
                Name = filter.Localized_Name,
                Options = new PersistableStringDictionary(
                    filter.Tags.ToDictionary(
                        x => x.Key,
                        x => x.Value.Localized_Name
                    )
                )
            };

            app.Filters.Add(newFilter);
            _db.SaveChanges();
            return newFilter;
        }

        public async Task<SteamStoreItem> AddOrUpdateAppStoreItemAsAvailable(SteamApp app, SteamCurrency currency, string languageId, AssetModel asset, DateTimeOffset timeChecked)
        {
            var dbItem = await _db.SteamStoreItems
                .Include(x => x.Stores)
                .Include(x => x.Description)
                .Where(x => x.AppId == app.Id)
                .FirstOrDefaultAsync(x => x.SteamId == asset.Name);

            if (dbItem != null)
            {
                // Item is now available again
                dbItem.IsAvailable = true;
                // Update prices
                // TODO: Move this to a seperate job (to avoid spam?)
                //if (asset.Prices != null)
                //{
                //    dbItem.Prices = new PersistablePriceDictionary(GetPriceTable(asset.Prices));
                //    dbItem.Price = dbItem.Prices.FirstOrDefault(x => x.Key == currency.Name).Value;
                //}
                return dbItem;
            }

            var importAssetDescription = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
            {
                AppId = ulong.Parse(app.SteamId),
                AssetClassId = asset.ClassId
            });
            var assetDescription = importAssetDescription.AssetDescription;
            if (assetDescription == null)
            {
                return null;
            }

            if (assetDescription.TimeAccepted == null)
            {
                assetDescription.TimeAccepted = timeChecked;
            }

            // TODO: This is creating duplicate items, need to find and re-use any existing items before creating new ones
            var prices = GetPriceTable(asset.Prices);
            app.StoreItems.Add(dbItem = new SteamStoreItem()
            {
                SteamId = asset.Name,
                AppId = app.Id,
                Description = assetDescription,
                Currency = currency,
                Price = (long)prices.FirstOrDefault(x => x.Key == currency.Name).Value,
                Prices = new PersistablePriceDictionary(prices),
                PricesAreLocked = true, // we are 100% confident that these are correct
                IsAvailable = true
            });

            return dbItem;
        }

        public IDictionary<string, long> GetPriceTable(AssetPricesModel prices)
        {
            return prices.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(
                    k => k.Name,
                    prop => (long)((uint)prop.GetValue(prices, null))
                );
        }

        public async Task<SteamMarketItem> UpdateSteamMarketItemOrders(SteamMarketItem item, Guid currencyId, SteamMarketItemOrdersHistogramJsonResponse histogram)
        {
            if (item == null || histogram?.Success != true)
            {
                return item;
            }

            // Lazy-load buy/sell order history if missing, required for recalculation
            if (item.BuyOrders?.Any() != true || item.SellOrders?.Any() != true)
            {
                item = await _db.SteamMarketItems
                    .Include(x => x.BuyOrders)
                    .Include(x => x.SellOrders)
                    .SingleOrDefaultAsync(x => x.Id == item.Id);
            }

            item.LastCheckedOrdersOn = DateTimeOffset.Now;
            item.CurrencyId = currencyId;
            item.RecalculateOrders(
                ParseSteamMarketItemOrdersFromGraph<SteamMarketItemBuyOrder>(histogram.BuyOrderGraph),
                histogram.BuyOrderCount.SteamQuantityValueAsInt(),
                ParseSteamMarketItemOrdersFromGraph<SteamMarketItemSellOrder>(histogram.SellOrderGraph),
                histogram.SellOrderCount.SteamQuantityValueAsInt()
            );

            return item;
        }

        public async Task<SteamMarketItem> UpdateSteamMarketItemSalesHistory(SteamMarketItem item, Guid currencyId, SteamMarketPriceHistoryJsonResponse sales)
        {
            if (item == null || sales?.Success != true)
            {
                return item;
            }

            // Lazy-load sales history if missing, required for recalculation
            if (item.SalesHistory?.Any() != true || item.Activity?.Any() != true)
            {
                item = await _db.SteamMarketItems
                    .Include(x => x.SalesHistory)
                    .Include(x => x.Activity)
                    .SingleOrDefaultAsync(x => x.Id == item.Id);
            }

            item.LastCheckedSalesOn = DateTimeOffset.Now;
            item.CurrencyId = currencyId;
            item.RecalculateSales(
                ParseSteamMarketItemSalesFromGraph(sales.Prices)
            );

            return item;
        }

        private T[] ParseSteamMarketItemOrdersFromGraph<T>(string[][] orderGraph)
            where T : Steam.Data.Store.SteamMarketItemOrder, new()
        {
            var orders = new List<T>();
            if (orderGraph == null)
            {
                return orders.ToArray();
            }

            var totalQuantity = 0;
            for (int i = 0; i < orderGraph.Length; i++)
            {
                var price = orderGraph[i][0].SteamPriceAsInt();
                var quantity = (orderGraph[i][1].SteamQuantityValueAsInt() - totalQuantity);
                orders.Add(new T()
                {
                    Price = price,
                    Quantity = quantity,
                });
                totalQuantity += quantity;
            }

            return orders.ToArray();
        }

        private SteamMarketItemSale[] ParseSteamMarketItemSalesFromGraph(string[][] salesGraph)
        {
            var sales = new List<SteamMarketItemSale>();
            if (salesGraph == null)
            {
                return sales.ToArray();
            }

            var totalQuantity = 0;
            for (int i = 0; i < salesGraph.Length; i++)
            {
                var timeStamp = DateTime.ParseExact(salesGraph[i][0], "MMM dd yyyy HH: z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                var price = salesGraph[i][1].SteamPriceAsInt();
                var quantity = salesGraph[i][2].SteamQuantityValueAsInt();
                sales.Add(new SteamMarketItemSale()
                {
                    Timestamp = timeStamp,
                    Price = price,
                    Quantity = quantity,
                });
                totalQuantity += quantity;
            }

            return sales.ToArray();
        }

        public async Task<IEnumerable<SteamMarketItem>> FindOrAddSteamMarketItems(IEnumerable<SteamMarketSearchItem> items, SteamCurrency currency)
        {
            var dbItems = new List<SteamMarketItem>();
            foreach (var item in items)
            {
                dbItems.Add(await FindOrAddSteamMarketItem(item, currency));
            }
            return dbItems;
        }

        public async Task<SteamMarketItem> FindOrAddSteamMarketItem(SteamMarketSearchItem item, SteamCurrency currency)
        {
            if (item?.AssetDescription?.AppId == null)
            {
                return null;
            }

            var dbApp = _db.SteamApps.FirstOrDefault(x => x.SteamId == item.AssetDescription.AppId.ToString());
            if (dbApp == null)
            {
                return null;
            }

            var dbItem = _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Include(x => x.Description.Icon)
                .FirstOrDefault(x => x.Description != null && x.Description.ClassId == item.AssetDescription.ClassId);

            if (dbItem != null)
            {
                return dbItem;
            }

            var importAssetDescription = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
            {
                AppId = ulong.Parse(dbApp.SteamId),
                AssetClassId = item.AssetDescription.ClassId,
                // TODO: Test this more. It seems there is missing data sometimes so we'll fetch the full details from Steam instead
                //AssetClass = item.AssetDescription 
            });
            var assetDescription = importAssetDescription.AssetDescription;
            if (assetDescription == null)
            {
                return null;
            }

            dbApp.MarketItems.Add(dbItem = new SteamMarketItem()
            {
                SteamId = assetDescription.NameId?.ToString(),
                App = dbApp,
                AppId = dbApp.Id,
                Description = assetDescription,
                DescriptionId = assetDescription.Id,
                Currency = currency,
                CurrencyId = currency.Id,
                Supply = item.SellListings,
                BuyNowPrice = item.SellPrice
            });

            return dbItem;
        }
    }
}
