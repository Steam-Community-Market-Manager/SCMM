using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Models;
using SCMM.Steam.Data.Models.Community.Requests.Blob;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using Steam.Models;
using Steam.Models.SteamEconomy;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SCMM.Steam.API
{
    public class SteamService
    {
        private readonly TimeSpan DefaultCachePeriod = TimeSpan.FromHours(6);

        private readonly SteamDbContext _db;
        private readonly SteamConfiguration _cfg;
        private readonly SteamCommunityClient _communityClient;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;


        public SteamService(SteamDbContext db, IConfiguration cfg, SteamCommunityClient communityClient, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _communityClient = communityClient;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        public Steam.Data.Store.SteamAssetFilter AddOrUpdateAppAssetFilter(SteamApp app, SCMM.Steam.Data.Models.Community.Models.SteamAssetFilter filter)
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

        public async Task<Steam.Data.Store.SteamAssetDescription> UpdateAssetDescription(Steam.Data.Store.SteamAssetDescription assetDescription, AssetClassInfoModel assetClass)
        {
            // Update tags
            if (assetClass.Tags != null)
            {
                foreach (var tag in assetClass.Tags)
                {
                    if (!assetDescription.Tags.ContainsKey(tag.Category))
                    {
                        assetDescription.Tags[tag.Category] = tag.Name;
                    }
                }
            }

            // Update name and description
            if (String.IsNullOrEmpty(assetDescription.Name) && !String.IsNullOrEmpty(assetClass.Name))
            {
                assetDescription.Name = assetClass.Name;
            }

            //if (String.IsNullOrEmpty(assetDescription.Description) && assetClass.Descriptions?.Count > 0)
            //{
            //    assetDescription.Description = assetClass.Descriptions.FirstOrDefault()?.Value;
            //}

            // Update icons
            if (String.IsNullOrEmpty(assetDescription.IconUrl) && !String.IsNullOrEmpty(assetClass.IconUrl?.ToString()))
            {
                assetDescription.IconUrl = assetClass.IconUrl.ToString();
            }
            if (assetDescription.IconId == null && !String.IsNullOrEmpty(assetDescription.IconUrl))
            {
                var fetchAndCreateImageData = await _commandProcessor.ProcessWithResultAsync(new FetchAndCreateImageDataRequest()
                {
                    Url = assetDescription.IconUrl,
                    UseExisting = true
                });
                if (fetchAndCreateImageData?.Image != null)
                {
                    assetDescription.Icon = fetchAndCreateImageData.Image;
                }
            }
            if (String.IsNullOrEmpty(assetDescription.IconLargeUrl) && !String.IsNullOrEmpty(assetClass.IconUrlLarge?.ToString()))
            {
                assetDescription.IconLargeUrl = assetClass.IconUrlLarge.ToString();
            }
            if (assetDescription.IconLargeId == null && !String.IsNullOrEmpty(assetDescription.IconLargeUrl))
            {
                var fetchAndCreateImageData = await _commandProcessor.ProcessWithResultAsync(new FetchAndCreateImageDataRequest()
                {
                    Url = assetDescription.IconLargeUrl,
                    UseExisting = true
                });
                if (fetchAndCreateImageData?.Image != null)
                {
                    assetDescription.IconLarge = fetchAndCreateImageData.Image;
                }
            }

            //switch (assetClass.Type)
            //{
            //    case "Workshop Item": break;
            //}

            switch (assetClass.Tradable)
            {
                case "1": assetDescription.Flags |= SCMM.Steam.Data.Models.Enums.SteamAssetDescriptionFlags.Tradable; break;
                case "0": assetDescription.Flags &= ~SCMM.Steam.Data.Models.Enums.SteamAssetDescriptionFlags.Tradable; break;
            }
            switch (assetClass.Marketable)
            {
                case "1": assetDescription.Flags |= SCMM.Steam.Data.Models.Enums.SteamAssetDescriptionFlags.Marketable; break;
                case "0": assetDescription.Flags &= ~SCMM.Steam.Data.Models.Enums.SteamAssetDescriptionFlags.Marketable; break;
            }
            switch (assetClass.Commodity)
            {
                case "1": assetDescription.Flags |= SCMM.Steam.Data.Models.Enums.SteamAssetDescriptionFlags.Commodity; break;
                case "0": assetDescription.Flags &= ~SCMM.Steam.Data.Models.Enums.SteamAssetDescriptionFlags.Commodity; break;
            }

            // Update last checked on
            assetDescription.LastCheckedOn = DateTimeOffset.Now;
            return assetDescription;
        }

        public async Task<Steam.Data.Store.SteamAssetDescription> UpdateAssetDescription(Steam.Data.Store.SteamAssetDescription assetDescription, PublishedFileDetailsModel publishedFile, bool updateSubscriptionGraph = false)
        {
            // Update asset description tags
            if (assetDescription != null && publishedFile.Tags != null)
            {
                foreach (var tag in publishedFile.Tags.Where(x => !Constants.SteamIgnoredWorkshopTags.Any(y => x == y)))
                {
                    var tagTrimmed = tag.Replace(" ", String.Empty).Trim();
                    var tagKey = $"{Constants.SteamAssetTagWorkshop}.{Char.ToLowerInvariant(tagTrimmed[0]) + tagTrimmed.Substring(1)}";
                    if (!assetDescription.Tags.ContainsKey(tagKey))
                    {
                        assetDescription.Tags[tagKey] = tag;
                    }
                }
            }

            // Update workshop infomation
            var workshopFile = assetDescription?.WorkshopFile;
            if (workshopFile != null)
            {
                // Update images
                if (String.IsNullOrEmpty(workshopFile.ImageUrl) && !String.IsNullOrEmpty(publishedFile.PreviewUrl?.ToString()))
                {
                    workshopFile.ImageUrl = publishedFile.PreviewUrl.ToString();
                }
                if (workshopFile.ImageId == null && !String.IsNullOrEmpty(workshopFile.ImageUrl))
                {
                    var fetchAndCreateImageData = await _commandProcessor.ProcessWithResultAsync(new FetchAndCreateImageDataRequest()
                    {
                        Url = workshopFile.ImageUrl,
                        UseExisting = true
                    });
                    if (fetchAndCreateImageData?.Image != null)
                    {
                        workshopFile.Image = fetchAndCreateImageData.Image;
                    }
                }

                // Update creator
                if (workshopFile.CreatorId == null && publishedFile.Creator > 0)
                {
                    var fetchAndCreateProfile = await _commandProcessor.ProcessWithResultAsync(new FetchAndCreateSteamProfileRequest()
                    {
                        ProfileId = publishedFile.Creator.ToString()
                    });
                    if (fetchAndCreateProfile?.Profile != null)
                    {
                        workshopFile.Creator = fetchAndCreateProfile.Profile;
                        if (!assetDescription.Tags.ContainsKey(Constants.SteamAssetTagCreator))
                        {
                            assetDescription.Tags[Constants.SteamAssetTagCreator] = fetchAndCreateProfile.Profile.Name;
                        }
                    }
                }

                // Update timestamps
                if (publishedFile.TimeCreated > DateTime.MinValue)
                {
                    workshopFile.CreatedOn = publishedFile.TimeCreated;
                }
                if (publishedFile.TimeUpdated > DateTime.MinValue)
                {
                    workshopFile.UpdatedOn = publishedFile.TimeUpdated;
                }
                if (workshopFile.AcceptedOn > DateTimeOffset.MinValue)
                {
                    if (!assetDescription.Tags.ContainsKey(Constants.SteamAssetTagAcceptedYear))
                    {
                        if (workshopFile.AcceptedOn.HasValue)
                        {
                            var culture = CultureInfo.InvariantCulture;
                            var acceptedOn = workshopFile.AcceptedOn.Value.UtcDateTime;
                            int acceptedOnWeek = culture.Calendar.GetWeekOfYear(acceptedOn, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);
                            assetDescription.Tags[Constants.SteamAssetTagAcceptedYear] = acceptedOn.ToString("yyyy");
                            assetDescription.Tags[Constants.SteamAssetTagAcceptedWeek] = $"Week {acceptedOnWeek}";
                        }
                    }
                }

                // Update subscriptions, favourites, views
                workshopFile.Subscriptions = (int)Math.Max(publishedFile.LifetimeSubscriptions, publishedFile.Subscriptions);
                workshopFile.Favourited = (int)Math.Max(publishedFile.LifetimeFavorited, publishedFile.Favorited);
                workshopFile.Views = (int)publishedFile.Views;
                if (updateSubscriptionGraph)
                {
                    var utcDate = DateTime.UtcNow.Date;
                    var maxSubscriptions = workshopFile.Subscriptions;
                    if (workshopFile.SubscriptionsGraph.ContainsKey(utcDate))
                    {
                        maxSubscriptions = (int)Math.Max(maxSubscriptions, workshopFile.SubscriptionsGraph[utcDate]);
                    }
                    workshopFile.SubscriptionsGraph[utcDate] = maxSubscriptions;
                    workshopFile.SubscriptionsGraph = new PersistableDailyGraphDataSet(
                        workshopFile.SubscriptionsGraph
                    );
                }

                // Update flags
                if (!workshopFile.Flags.HasFlag(SCMM.Steam.Data.Models.Enums.SteamAssetWorkshopFileFlags.Banned) && publishedFile.Banned)
                {
                    workshopFile.Flags |= SCMM.Steam.Data.Models.Enums.SteamAssetWorkshopFileFlags.Banned;
                    workshopFile.BanReason = publishedFile.BanReason;
                }
                if (workshopFile.Flags.HasFlag(SCMM.Steam.Data.Models.Enums.SteamAssetWorkshopFileFlags.Banned) && !publishedFile.Banned)
                {
                    workshopFile.Flags &= ~SCMM.Steam.Data.Models.Enums.SteamAssetWorkshopFileFlags.Banned;
                    workshopFile.BanReason = null;
                }
            }

            // Update last checked on
            workshopFile.LastCheckedOn = DateTimeOffset.Now;
            return assetDescription;
        }

        public SteamStoreItemItemStore UpdateStoreItemIndex(SteamStoreItemItemStore storeItem, int storeIndex)
        {
            var utcDateTime = (DateTime.UtcNow.Date + TimeSpan.FromHours(DateTime.UtcNow.TimeOfDay.Hours));
            storeItem.Index = storeIndex;
            storeItem.IndexGraph[utcDateTime] = storeIndex;
            storeItem.IndexGraph = new PersistableHourlyGraphDataSet(
                storeItem.IndexGraph
            );

            return storeItem;
        }

        ///
        /// UPDATE BELOW...
        ///

        public async Task<SteamAssetWorkshopFile> AddOrUpdateAssetWorkshopFile(SteamApp app, string fileId)
        {
            var dbWorkshopFile = await _db.SteamAssetWorkshopFiles
                .Where(x => x.SteamId == fileId)
                .FirstOrDefaultAsync();

            if (dbWorkshopFile != null)
            {
                return dbWorkshopFile;
            }

            dbWorkshopFile = new SteamAssetWorkshopFile()
            {
                SteamId = fileId,
                AppId = app.Id
            };

            _db.SteamAssetWorkshopFiles.Add(dbWorkshopFile);
            _db.SaveChanges();
            return dbWorkshopFile;
        }

        public async Task<Steam.Data.Store.SteamAssetDescription> AddOrUpdateAssetDescription(SteamApp app, string languageId, ulong classId)
        {
            var dbAssetDescription = await _db.SteamAssetDescriptions
                .Include(x => x.WorkshopFile)
                .Where(x => x.SteamId == classId.ToString())
                .FirstOrDefaultAsync();

            if (dbAssetDescription != null)
            {
                return dbAssetDescription;
            }

            var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_cfg.ApplicationKey);
            var steamEconomy = steamWebInterfaceFactory.CreateSteamWebInterface<SteamEconomy>();
            var response = await steamEconomy.GetAssetClassInfoAsync(
                UInt32.Parse(app.SteamId), new List<ulong>() { classId }, languageId
            );
            if (response?.Data?.Success != true)
            {
                return null;
            }

            var assetDescription = response.Data.AssetClasses.FirstOrDefault(x => x.ClassId == classId);
            if (assetDescription == null)
            {
                return null;
            }

            var tags = new Dictionary<string, string>();
            var workshopFile = (SteamAssetWorkshopFile)null;
            var workshopFileId = (string)null;
            var viewWorkshopAction = assetDescription?.Actions?.FirstOrDefault(x => x.Name == Constants.SteamActionViewWorkshopItem);
            if (viewWorkshopAction != null)
            {
                var workshopFileIdGroups = Regex.Match(viewWorkshopAction.Link, Constants.SteamActionViewWorkshopItemRegex).Groups;
                workshopFileId = (workshopFileIdGroups.Count > 1) ? workshopFileIdGroups[1].Value : "0";
                workshopFile = await AddOrUpdateAssetWorkshopFile(app, workshopFileId);
            }

            dbAssetDescription = new Steam.Data.Store.SteamAssetDescription()
            {
                SteamId = assetDescription.ClassId.ToString(),
                AppId = app.Id,
                Name = assetDescription.MarketName,
                BackgroundColour = assetDescription.BackgroundColor.SteamColourToHexString(),
                ForegroundColour = assetDescription.NameColor.SteamColourToHexString(),
                IconUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrl),
                IconLargeUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrlLarge ?? assetDescription.IconUrl),
                WorkshopFile = workshopFile,
                Tags = new PersistableStringDictionary(tags)
            };

            if (dbAssetDescription.IconId == null && !String.IsNullOrEmpty(dbAssetDescription.IconUrl))
            {
                var fetchAndCreateImageData = await _commandProcessor.ProcessWithResultAsync(new FetchAndCreateImageDataRequest()
                {
                    Url = dbAssetDescription.IconUrl,
                    UseExisting = true
                });
                if (fetchAndCreateImageData?.Image != null)
                {
                    dbAssetDescription.Icon = fetchAndCreateImageData.Image;
                }
            }
            if (dbAssetDescription.IconLargeId == null && !String.IsNullOrEmpty(dbAssetDescription.IconLargeUrl))
            {
                var fetchAndCreateImageData = await _commandProcessor.ProcessWithResultAsync(new FetchAndCreateImageDataRequest()
                {
                    Url = dbAssetDescription.IconLargeUrl,
                    UseExisting = true
                });
                if (fetchAndCreateImageData?.Image != null)
                {
                    dbAssetDescription.IconLarge = fetchAndCreateImageData.Image;
                }
            }

            _db.SteamAssetDescriptions.Add(dbAssetDescription);
            _db.SaveChanges();
            return dbAssetDescription;
        }

        public async Task<Steam.Data.Store.SteamAssetDescription> AddOrUpdateAssetDescription(SteamApp app, SCMM.Steam.Data.Models.Community.Models.SteamAssetDescription assetDescription)
        {
            var dbAssetDescription = await _db.SteamAssetDescriptions
                .Where(x => x.SteamId == assetDescription.ClassId)
                .Include(x => x.WorkshopFile)
                .FirstOrDefaultAsync();

            if (dbAssetDescription != null)
            {
                return dbAssetDescription;
            }

            var tags = new Dictionary<string, string>();
            var workshopFile = (SteamAssetWorkshopFile)null;
            var workshopFileId = (string)null;
            var viewWorkshopAction = assetDescription?.Actions?.FirstOrDefault(x => x.Name == Constants.SteamActionViewWorkshopItem);
            if (viewWorkshopAction != null)
            {
                var workshopFileIdGroups = Regex.Match(viewWorkshopAction.Link, Constants.SteamActionViewWorkshopItemRegex).Groups;
                workshopFileId = (workshopFileIdGroups.Count > 1) ? workshopFileIdGroups[1].Value : "0";
                workshopFile = await AddOrUpdateAssetWorkshopFile(app, workshopFileId);
            }

            dbAssetDescription = new Steam.Data.Store.SteamAssetDescription()
            {
                SteamId = assetDescription.ClassId.ToString(),
                AppId = app.Id,
                Name = assetDescription.MarketName,
                BackgroundColour = assetDescription.BackgroundColor.SteamColourToHexString(),
                ForegroundColour = assetDescription.NameColor.SteamColourToHexString(),
                IconUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrl),
                IconLargeUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrlLarge ?? assetDescription.IconUrl),
                WorkshopFile = workshopFile,
                Tags = new PersistableStringDictionary(tags)
            };

            _db.SteamAssetDescriptions.Add(dbAssetDescription);
            _db.SaveChanges();
            return dbAssetDescription;
        }

        public async Task<SteamStoreItem> AddOrUpdateAppStoreItem(SteamApp app, SteamCurrency currency, string languageId, AssetModel asset, DateTimeOffset timeChecked)
        {
            var dbItem = await _db.SteamStoreItems
                .Include(x => x.Stores)
                .Include(x => x.Description)
                .Where(x => x.AppId == app.Id)
                .FirstOrDefaultAsync(x => x.SteamId == asset.Name);

            if (dbItem != null)
            {
                // Update prices
                // TODO: Move this to a seperate job (to avoid spam?)
                //if (asset.Prices != null)
                //{
                //    dbItem.Prices = new PersistablePriceDictionary(GetPriceTable(asset.Prices));
                //    dbItem.Price = dbItem.Prices.FirstOrDefault(x => x.Key == currency.Name).Value;
                //}
                return dbItem;
            }

            var assetDescription = await AddOrUpdateAssetDescription(app, languageId, asset.ClassId);
            if (assetDescription == null)
            {
                return null;
            }

            if (assetDescription?.WorkshopFile != null)
            {
                assetDescription.WorkshopFile.AcceptedOn = timeChecked;
            }

            var prices = GetPriceTable(asset.Prices);
            app.StoreItems.Add(dbItem = new SteamStoreItem()
            {
                SteamId = asset.Name,
                AppId = app.Id,
                Description = assetDescription,
                Prices = new PersistablePriceDictionary(prices),
                Price = prices.FirstOrDefault(x => x.Key == currency.Name).Value
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

        ///
        /// UPDATE BELOW...
        ///

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
            if (String.IsNullOrEmpty(item?.AssetDescription.AppId))
            {
                return null;
            }

            var dbApp = _db.SteamApps.FirstOrDefault(x => x.SteamId == item.AssetDescription.AppId);
            if (dbApp == null)
            {
                return null;
            }

            var dbItem = _db.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                .Include(x => x.Description.Icon)
                .FirstOrDefault(x => x.Description != null && x.Description.SteamId == item.AssetDescription.ClassId);

            if (dbItem != null)
            {
                return dbItem;
            }

            var assetDescription = await AddOrUpdateAssetDescription(dbApp, item.AssetDescription);
            if (assetDescription == null)
            {
                return null;
            }

            dbApp.MarketItems.Add(dbItem = new SteamMarketItem()
            {
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

        public SteamMarketItem UpdateMarketItemNameId(SteamMarketItem item, string itemNameId)
        {
            if (!String.IsNullOrEmpty(itemNameId))
            {
                item.SteamId = itemNameId;
                _db.SaveChanges();
            }

            return item;
        }
    }
}
