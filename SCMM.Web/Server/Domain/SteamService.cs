using AngleSharp.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Steam.Client;
using SCMM.Steam.Shared;
using SCMM.Steam.Shared.Community.Models;
using SCMM.Steam.Shared.Community.Requests.Blob;
using SCMM.Steam.Shared.Community.Requests.Html;
using SCMM.Steam.Shared.Community.Requests.Json;
using SCMM.Steam.Shared.Community.Responses.Json;
using SCMM.Web.Client;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain.Models.Steam;
using Steam.Models;
using Steam.Models.SteamEconomy;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Domain
{
    public class SteamService
    {
        private readonly SteamDbContext _db;
        private readonly SteamConfiguration _cfg;

        public SteamService(SteamDbContext db, IConfiguration cfg)
        {
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
        }

        public async Task<Models.Steam.SteamProfile> AddOrUpdateSteamProfile(string steamId)
        {
            var profile = await _db.SteamProfiles.FirstOrDefaultAsync(x => x.SteamId == steamId);
            if (profile != null)
            {
                // Nothing to update
                return profile;
            }

            var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_cfg.ApplicationKey);
            var steamUser = steamWebInterfaceFactory.CreateSteamWebInterface<SteamUser>();
            var response = await steamUser.GetPlayerSummaryAsync(UInt64.Parse(steamId));
            if (response?.Data == null)
            {
                return null;
            }

            var profileId = response.Data.ProfileUrl;
            if (!String.IsNullOrEmpty(profileId))
            {
                profileId = (Regex.Match(profileId, @"id\/(.*)\/").Groups.OfType<Capture>().LastOrDefault()?.Value ?? profileId);
            }
            if (String.IsNullOrEmpty(profileId))
            {
                profileId = (Regex.Match(profileId, @"id\/(.*)\/").Groups.OfType<Capture>().LastOrDefault()?.Value ?? profileId);
            }

            profile = new Models.Steam.SteamProfile()
            {
                SteamId = steamId,
                ProfileId = profileId,
                Name = response.Data.Nickname,
                AvatarUrl = response.Data.AvatarUrl,
                AvatarLargeUrl = response.Data.AvatarFullUrl,
                Country = response.Data.CountryCode
            };

            _db.SteamProfiles.Add(profile);
            await _db.SaveChangesAsync();
            return profile;
        }

        public async Task<Models.Steam.SteamAssetFilter> AddOrUpdateAppAssetFilter(SteamApp app, Steam.Shared.Community.Models.SteamAssetFilter filter)
        {
            var existingFilter = app.Filters.FirstOrDefault(x => x.SteamId == filter.Name);
            if (existingFilter != null)
            {
                // Nothing to update
                return existingFilter;
            }

            var newFilter = new Models.Steam.SteamAssetFilter()
            {
                SteamId = filter.Name,
                Name = filter.Localized_Name,
                Options = new Data.Types.PersistableStringDictionary(
                    filter.Tags.ToDictionary(
                        x => x.Key,
                        x => x.Value.Localized_Name
                    )
                )
            };

            app.Filters.Add(newFilter);
            await _db.SaveChangesAsync();
            return newFilter;
        }
        
        public async Task<Models.Steam.SteamAssetDescription> UpdateAssetDescription(Models.Steam.SteamAssetDescription assetDescription, AssetClassInfoModel assetClass)
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

            return assetDescription;
        }

        public async Task<Models.Steam.SteamAssetDescription> UpdateAssetDescription(Models.Steam.SteamAssetDescription assetDescription, PublishedFileDetailsModel publishedFile, bool updateSubscriptionGraph = false)
        {
            // Update workshop tags
            if (publishedFile.Tags != null)
            {
                foreach (var tag in publishedFile.Tags.Where(x => !SteamConstants.SteamIgnoredWorkshopTags.Any(y => x == y)))
                {
                    var tagTrimmed = tag.Replace(" ", String.Empty).Trim();
                    var tagKey = $"{SteamConstants.SteamAssetTagWorkshop}.{Char.ToLowerInvariant(tagTrimmed[0]) + tagTrimmed.Substring(1)}";
                    if (!assetDescription.Tags.ContainsKey(tagKey))
                    {
                        assetDescription.Tags[tagKey] = tag;
                    }
                }
            }

            // Update workshop statistics
            var workshopFile = assetDescription.WorkshopFile;
            if (workshopFile != null)
            {
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
                    if (!assetDescription.Tags.ContainsKey(SteamConstants.SteamAssetTagAcceptedYear))
                    {
                        var culture = CultureInfo.InvariantCulture;
                        var acceptedOn = workshopFile.AcceptedOn.UtcDateTime;
                        int acceptedOnWeek = culture.Calendar.GetWeekOfYear(acceptedOn, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);
                        assetDescription.Tags[SteamConstants.SteamAssetTagAcceptedYear] = acceptedOn.ToString("yyyy");
                        assetDescription.Tags[SteamConstants.SteamAssetTagAcceptedWeek] = $"Week {acceptedOnWeek}";
                    }
                }

                workshopFile.Subscriptions = (int) Math.Max(publishedFile.LifetimeSubscriptions, publishedFile.Subscriptions);
                if (updateSubscriptionGraph)
                {
                    var utcDate = DateTime.UtcNow.Date;
                    var maxSubscriptions = workshopFile.Subscriptions;
                    if (workshopFile.SubscriptionsGraph.ContainsKey(utcDate))
                    {
                        maxSubscriptions = (int) Math.Max(maxSubscriptions, workshopFile.SubscriptionsGraph[utcDate]);
                    }
                    workshopFile.SubscriptionsGraph[utcDate] = maxSubscriptions;
                    workshopFile.SubscriptionsGraph = new Data.Types.PersistableGraphDataSet(
                        workshopFile.SubscriptionsGraph
                    );
                }

                workshopFile.Favourited = (int)Math.Max(publishedFile.LifetimeFavorited, publishedFile.Favorited);
                workshopFile.Views = (int)publishedFile.Views;
                workshopFile.LastCheckedOn = DateTimeOffset.Now;

                if (workshopFile.CreatorId == null)
                {
                    workshopFile.Creator = await AddOrUpdateSteamProfile(publishedFile.Creator.ToString());
                    if (workshopFile.Creator != null)
                    {
                        if (!assetDescription.Tags.ContainsKey(SteamConstants.SteamAssetTagCreator))
                        {
                            assetDescription.Tags[SteamConstants.SteamAssetTagCreator] = workshopFile.Creator.Name;
                        }
                    }
                }
            }

            return assetDescription;
        }

        ///
        /// UPDATE BELOW...
        ///

        public async Task<Models.Steam.SteamAssetWorkshopFile> AddOrUpdateAssetWorkshopFile(SteamApp app, string fileId)
        {
            var dbWorkshopFile = await _db.SteamAssetWorkshopFiles
                .Where(x => x.SteamId == fileId)
                .FirstOrDefaultAsync();

            if (dbWorkshopFile != null)
            {
                return dbWorkshopFile;
            }

            dbWorkshopFile = new Models.Steam.SteamAssetWorkshopFile()
            {
                SteamId = fileId,
                AppId = app.Id
            };

            _db.SteamAssetWorkshopFiles.Add(dbWorkshopFile);
            await _db.SaveChangesAsync();
            return dbWorkshopFile;
        }

        public async Task<Models.Steam.SteamAssetDescription> AddOrUpdateAssetDescription(SteamApp app, SteamLanguage language, ulong classId)
        {
            var dbAssetDescription = await _db.SteamAssetDescriptions
                .Where(x => x.SteamId == classId.ToString())
                .Include(x => x.WorkshopFile)
                .FirstOrDefaultAsync();

            if (dbAssetDescription != null)
            {
                return dbAssetDescription;
            }

            var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_cfg.ApplicationKey);
            var steamEconomy = steamWebInterfaceFactory.CreateSteamWebInterface<SteamEconomy>();
            var response = await steamEconomy.GetAssetClassInfoAsync(
                UInt32.Parse(app.SteamId), new List<ulong>() { classId }, language.SteamId
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
            var viewWorkshopAction = assetDescription?.Actions?.FirstOrDefault(x => x.Name == SteamConstants.SteamActionViewWorkshopItem);
            if (viewWorkshopAction != null)
            {
                var workshopFileIdGroups = Regex.Match(viewWorkshopAction.Link, SteamConstants.SteamActionViewWorkshopItemRegex).Groups;
                workshopFileId = (workshopFileIdGroups.Count > 1) ? workshopFileIdGroups[1].Value : "0";
                workshopFile = await AddOrUpdateAssetWorkshopFile(app, workshopFileId);
            }

            dbAssetDescription = new Models.Steam.SteamAssetDescription()
            {
                SteamId = assetDescription.ClassId.ToString(),
                AppId = app.Id,
                Name = assetDescription.MarketName,
                BackgroundColour = assetDescription.BackgroundColor.SteamColourToHexString(),
                ForegroundColour = assetDescription.NameColor.SteamColourToHexString(),
                IconUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrl).Uri.ToString(),
                IconLargeUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrlLarge).Uri.ToString(),
                WorkshopFile = workshopFile,
                Tags = new Data.Types.PersistableStringDictionary(tags)
            };

            _db.SteamAssetDescriptions.Add(dbAssetDescription);
            await _db.SaveChangesAsync();
            return dbAssetDescription;
        }

        public async Task<Models.Steam.SteamAssetDescription> AddOrUpdateAssetDescription(SteamApp app, Steam.Shared.Community.Models.SteamAssetDescription assetDescription)
        {
            var dbAssetDescription = await _db.SteamAssetDescriptions
                .Where(x => x.SteamId == assetDescription.ToString())
                .Include(x => x.WorkshopFile)
                .FirstOrDefaultAsync();

            if (dbAssetDescription != null)
            {
                return dbAssetDescription;
            }

            var tags = new Dictionary<string, string>();
            var workshopFile = (SteamAssetWorkshopFile)null;
            var workshopFileId = (string)null;
            var viewWorkshopAction = assetDescription?.Actions?.FirstOrDefault(x => x.Name == SteamConstants.SteamActionViewWorkshopItem);
            if (viewWorkshopAction != null)
            {
                var workshopFileIdGroups = Regex.Match(viewWorkshopAction.Link, SteamConstants.SteamActionViewWorkshopItemRegex).Groups;
                workshopFileId = (workshopFileIdGroups.Count > 1) ? workshopFileIdGroups[1].Value : "0";
                workshopFile = await AddOrUpdateAssetWorkshopFile(app, workshopFileId);
            }

            dbAssetDescription = new Models.Steam.SteamAssetDescription()
            {
                SteamId = assetDescription.ClassId.ToString(),
                AppId = app.Id,
                Name = assetDescription.MarketName,
                BackgroundColour = assetDescription.BackgroundColor.SteamColourToHexString(),
                ForegroundColour = assetDescription.NameColor.SteamColourToHexString(),
                IconUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrl).Uri.ToString(),
                IconLargeUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrlLarge).Uri.ToString(),
                WorkshopFile = workshopFile,
                Tags = new Data.Types.PersistableStringDictionary(tags)
            };

            _db.SteamAssetDescriptions.Add(dbAssetDescription);
            await _db.SaveChangesAsync();
            return dbAssetDescription;
        }

        public async Task<Models.Steam.SteamStoreItem> AddOrUpdateAppStoreItem(SteamApp app, SteamCurrency currency, SteamLanguage language, AssetModel asset, DateTime timeChecked)
        {
            var dbItem = await _db.SteamStoreItems
                .Include(x => x.Description)
                .Where(x => x.AppId == app.Id)
                .FirstOrDefaultAsync(x => x.SteamId == asset.Name);

            if (dbItem != null)
            {
                return dbItem;
            }

            var assetDescription = await AddOrUpdateAssetDescription(app, language, asset.ClassId);
            if (assetDescription == null)
            {
                return null;
            }

            if (assetDescription?.WorkshopFile != null)
            {
                assetDescription.WorkshopFile.AcceptedOn = timeChecked;
            }

            app.StoreItems.Add(dbItem = new Models.Steam.SteamStoreItem()
            {
                SteamId = asset.Name,
                AppId = app.Id,
                Description = assetDescription,
                Currency = currency,
                StorePrice = Int32.Parse(asset.Prices.ToDictionary().FirstOrDefault(x => x.Key == currency.Name).Value ?? "0")
            });

            return dbItem;
        }

        public async Task<Models.Steam.SteamMarketItem> UpdateSteamMarketItemOrders(SteamMarketItem item, Guid currencyId, SteamMarketItemOrdersHistogramJsonResponse histogram)
        {
            if (item == null || histogram?.Success != true)
            {
                return item;
            }

            item.LastCheckedOrdersOn = DateTimeOffset.Now;
            item.CurrencyId = currencyId;
            item.RecalculateOrders(
                ParseSteamMarketItemOrdersFromGraph(histogram.BuyOrderGraph),
                ParseSteamMarketItemOrdersFromGraph(histogram.SellOrderGraph)
            );

            return item;
        }

        public async Task<Models.Steam.SteamMarketItem> UpdateSteamMarketItemSalesHistory(SteamMarketItem item, Guid currencyId, SteamMarketPriceHistoryJsonResponse sales)
        {
            if (item == null || sales?.Success != true)
            {
                return item;
            }

            item.LastCheckedSalesOn = DateTimeOffset.Now;
            item.CurrencyId = currencyId;
            item.RecalculateSales(
                ParseSteamMarketItemSalesFromGraph(sales.Prices)
            );

            return item;
        }

        private Models.Steam.SteamMarketItemOrder[] ParseSteamMarketItemOrdersFromGraph(string[][] orderGraph)
        {
            var orders = new List<Models.Steam.SteamMarketItemOrder>();
            if (orderGraph == null)
            {
                return orders.ToArray();
            }

            var totalQuantity = 0;
            for (int i = 0; i < orderGraph.Length; i++)
            {
                var price = SteamEconomyHelper.GetPriceValueAsInt(orderGraph[i][0]);
                var quantity = (SteamEconomyHelper.GetQuantityValueAsInt(orderGraph[i][1]) - totalQuantity);
                orders.Add(new Models.Steam.SteamMarketItemOrder()
                {
                    Price = price,
                    Quantity = quantity,
                });
                totalQuantity += quantity;
            }

            return orders.ToArray();
        }

        private Models.Steam.SteamMarketItemSale[] ParseSteamMarketItemSalesFromGraph(string[][] salesGraph)
        {
            var sales = new List<Models.Steam.SteamMarketItemSale>();
            if (salesGraph == null)
            {
                return sales.ToArray();
            }

            var totalQuantity = 0;
            for (int i = 0; i < salesGraph.Length; i++)
            {
                var timeStamp = DateTime.ParseExact(salesGraph[i][0], "MMM dd yyyy HH: z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                var price = SteamEconomyHelper.GetPriceValueAsInt(salesGraph[i][1]);
                var quantity = SteamEconomyHelper.GetQuantityValueAsInt(salesGraph[i][2]);
                sales.Add(new Models.Steam.SteamMarketItemSale()
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

        public async Task<IEnumerable<SteamMarketItem>> FindOrAddSteamMarketItems(IEnumerable<SteamMarketSearchItem> items)
        {
            var dbItems = new List<SteamMarketItem>();
            foreach (var item in items)
            {
                dbItems.Add(await FindOrAddSteamMarketItem(item));
            }
            return dbItems;
        }

        public async Task<SteamMarketItem> FindOrAddSteamMarketItem(SteamMarketSearchItem item)
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
                .Include(x => x.Description)
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
                AppId = dbApp.Id,
                Description = assetDescription
            });

            return dbItem;
        }

        public async Task<SteamMarketItem> UpdateMarketItemNameId(SteamMarketItem item, string itemNameId)
        {
            if (!String.IsNullOrEmpty(itemNameId))
            {
                item.SteamId = itemNameId;
                await _db.SaveChangesAsync();
            }

            return item;
        }
    }
}
