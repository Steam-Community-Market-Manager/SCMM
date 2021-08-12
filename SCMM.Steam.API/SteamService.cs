using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
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

        public async Task<SteamStoreItem> AddOrUpdateStoreItemAndMarkAsAvailable(SteamApp app, SteamCurrency currency, AssetModel asset, DateTimeOffset timeChecked)
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
            var prices = ParseStoreItemPriceTable(asset.Prices);
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

        public async Task<SteamMarketItem> AddOrUpdateMarketItem(SteamApp app, SteamCurrency currency, SteamMarketPriceOverviewJsonResponse marketPriceOverview, SteamAssetDescription asset)
        {
            var dbItem = await _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Where(x => x.AppId == app.Id)
                .FirstOrDefaultAsync(x => x.Description.ClassId == asset.ClassId);

            if (dbItem != null)
            {
                return dbItem;
            }

            var importAssetDescription = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
            {
                AppId = ulong.Parse(app.SteamId),
                AssetClassId = asset.ClassId
            });
            var assetDescription = importAssetDescription.AssetDescription;
            if (assetDescription == null || assetDescription.NameId == null)
            {
                return null;
            }

            app.MarketItems.Add(dbItem = new SteamMarketItem()
            {
                SteamId = assetDescription.NameId?.ToString(),
                AppId = app.Id,
                Description = assetDescription,
                Currency = currency,
                Supply = marketPriceOverview.Volume.SteamQuantityValueAsInt(),
                BuyNowPrice = marketPriceOverview.LowestPrice.SteamPriceAsInt()
            });

            return dbItem;
        }

        public async Task<SteamMarketItem> UpdateMarketItemOrders(SteamMarketItem item, Guid currencyId, SteamMarketItemOrdersHistogramJsonResponse histogram)
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
                ParseMarketItemOrdersFromGraph<SteamMarketItemBuyOrder>(histogram.BuyOrderGraph),
                histogram.BuyOrderCount.SteamQuantityValueAsInt(),
                ParseMarketItemOrdersFromGraph<SteamMarketItemSellOrder>(histogram.SellOrderGraph),
                histogram.SellOrderCount.SteamQuantityValueAsInt()
            );

            return item;
        }

        public async Task<SteamMarketItem> UpdateMarketItemSalesHistory(SteamMarketItem item, Guid currencyId, SteamMarketPriceHistoryJsonResponse sales)
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
                ParseMarketItemSalesFromGraph(sales.Prices)
            );

            return item;
        }

        private IDictionary<string, long> ParseStoreItemPriceTable(AssetPricesModel prices)
        {
            return prices.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(
                    k => k.Name,
                    prop => (long)((uint)prop.GetValue(prices, null))
                );
        }

        private T[] ParseMarketItemOrdersFromGraph<T>(string[][] orderGraph)
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

        private SteamMarketItemSale[] ParseMarketItemSalesFromGraph(string[][] salesGraph)
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
    }
}
