using AngleSharp.Common;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Client;
using SCMM.Steam.Shared;
using SCMM.Steam.Shared.Community.Models;
using SCMM.Steam.Shared.Community.Requests.Blob;
using SCMM.Steam.Shared.Community.Requests.Html;
using SCMM.Steam.Shared.Community.Requests.Json;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain.Models.Steam;
using Steam.Models;
using Steam.Models.SteamEconomy;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Domain
{
    public static class SteamService
    {
        public static async Task<Models.Steam.SteamProfile> AddOrUpdateSteamProfile(SteamDbContext db, string steamId, string applicationKey)
        {
            var profile = await db.SteamProfiles.FirstOrDefaultAsync(x => x.SteamId == steamId);
            if (profile != null)
            {
                // No update required
                return profile;
            }

            var steamWebInterfaceFactory = new SteamWebInterfaceFactory(applicationKey);
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

            db.SteamProfiles.Add(profile);
            await db.SaveChangesAsync();
            return profile;
        }

        public static async Task<Models.Steam.SteamAssetFilter> AddOrUpdateAppAssetFilter(SteamDbContext db, SteamApp app, Steam.Shared.Community.Models.SteamAssetFilter filter)
        {
            var existingFilter = app.Filters.FirstOrDefault(x => x.SteamId == filter.Name);
            if (existingFilter != null)
            {
                // No update required
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
            await db.SaveChangesAsync();
            return newFilter;
        }

        public static async Task<Models.Steam.SteamAssetDescription> UpdateAssetWorkshopStatistics(SteamDbContext db, Models.Steam.SteamAssetDescription assetDescription, PublishedFileDetailsModel publishedFile, string applicationKey)
        {
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
                    assetDescription.Tags[SteamConstants.SteamAssetTagAccepted] = workshopFile.AcceptedOn.ToString("dd-MMM-yy");
                }
                workshopFile.Subscriptions = (int) Math.Max(publishedFile.LifetimeSubscriptions, publishedFile.Subscriptions);
                workshopFile.Favourited = (int)Math.Max(publishedFile.LifetimeFavorited, publishedFile.Favorited);
                workshopFile.Views = (int)publishedFile.Views;
                workshopFile.LastCheckedOn = DateTimeOffset.Now;
                if (workshopFile.CreatorId == null)
                {
                    workshopFile.Creator = await AddOrUpdateSteamProfile(db, publishedFile.Creator.ToString(), applicationKey);
                    if (workshopFile.Creator != null)
                    {
                        assetDescription.Tags[SteamConstants.SteamAssetTagCreator] = workshopFile.Creator.Name;
                    }
                }
            }

            if (publishedFile.Tags != null)
            {
                foreach(var tag in publishedFile.Tags)
                {
                    var tagTrimmed = tag.Replace(" ", String.Empty).Trim();
                    var tagKey = $"{SteamConstants.SteamAssetTagWorkshop}.{Char.ToLowerInvariant(tagTrimmed[0]) + tagTrimmed.Substring(1)}";
                    if (!assetDescription.Tags.ContainsKey(tagKey))
                    {
                        assetDescription.Tags[tagKey] = tag;
                    }
                }
            }

            return assetDescription;
        }

        ///
        /// UPDATE BELOW...
        ///

        public static async Task<Models.Steam.SteamAssetDescription> AddOrUpdateAssetDescription(SteamDbContext db, SteamApp app, SteamLanguage language, ulong classId, string applicationKey)
        {
            var dbAssetDescription = await db.SteamAssetDescriptions
                .Where(x => x.SteamId == classId.ToString())
                .FirstOrDefaultAsync();

            if (dbAssetDescription != null)
            {
                return dbAssetDescription;
            }

            var steamWebInterfaceFactory = new SteamWebInterfaceFactory(applicationKey);
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
                workshopFile = new Models.Steam.SteamAssetWorkshopFile()
                {
                    SteamId = workshopFileId
                };
            }

            dbAssetDescription = new Models.Steam.SteamAssetDescription()
            {
                SteamId = assetDescription.ClassId.ToString(),
                Name = assetDescription.MarketName,
                BackgroundColour = assetDescription.BackgroundColor.SteamColourToHexString(),
                ForegroundColour = assetDescription.NameColor.SteamColourToHexString(),
                IconUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrl).Uri.ToString(),
                IconLargeUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrlLarge).Uri.ToString(),
                WorkshopFile = workshopFile,
                Tags = new Data.Types.PersistableStringDictionary(tags)
            };

            db.SteamAssetDescriptions.Add(dbAssetDescription);
            await db.SaveChangesAsync();
            return dbAssetDescription;
        }

        public static async Task<Models.Steam.SteamStoreItem> AddOrUpdateAppStoreItem(SteamDbContext db, SteamApp app, SteamCurrency currency, SteamLanguage language, AssetModel asset, string applicationKey)
        {
            var dbItem = await db.SteamStoreItems
                .Include(x => x.Description)
                .Where(x => x.AppId == app.Id)
                .FirstOrDefaultAsync(x => x.SteamId == asset.Name);

            if (dbItem != null)
            {
                return dbItem;
            }

            var assetDescription = await AddOrUpdateAssetDescription(db, app, language, asset.ClassId, applicationKey);
            if (assetDescription == null)
            {
                return null;
            }

            app.StoreItems.Add(dbItem = new Models.Steam.SteamStoreItem()
            {
                SteamId = asset.Name,
                App = app,
                Description = assetDescription,
                Currency = currency,
                StorePrice = Int32.Parse(asset.Prices.ToDictionary().FirstOrDefault(x => x.Key == currency.Name).Value ?? "0")
            });

            return dbItem;
        }

        ///
        /// UPDATE BELOW...
        ///

        public static IEnumerable<SteamMarketItem> FindOrAddSteamItems(SteamDbContext db, IEnumerable<SteamMarketSearchItem> items)
        {
            foreach (var item in items)
            {
                yield return FindOrAddSteamItem(db, item);
            }
        }

        public static SteamMarketItem FindOrAddSteamItem(SteamDbContext db, SteamMarketSearchItem item)
        {
            if (String.IsNullOrEmpty(item?.AssetDescription.AppId))
            {
                return null;
            }

            var dbApp = db.SteamApps.FirstOrDefault(x => x.SteamId == item.AssetDescription.AppId);
            if (dbApp == null)
            {
                return null;
            }

            var dbItem = db.SteamMarketItems
                .Include(x => x.Description)
                .FirstOrDefault(x => x.Description != null && x.Description.SteamId == item.AssetDescription.ClassId);

            if (dbItem != null)
            {
                return dbItem;
            }

            var workshopFileId = (string)null;
            var viewWorkshopAction = item.AssetDescription?.Actions?.FirstOrDefault(x => x.Name == SteamConstants.SteamActionViewWorkshopItem);
            if (viewWorkshopAction != null)
            {
                var workshopFileIdGroups = Regex.Match(viewWorkshopAction.Link, SteamConstants.SteamActionViewWorkshopItemRegex).Groups;
                workshopFileId = (workshopFileIdGroups.Count > 1) ? workshopFileIdGroups[1].Value : "0";
            }

            dbApp.MarketItems.Add(dbItem = new SteamMarketItem()
            {
                App = dbApp,
                Description = new Domain.Models.Steam.SteamAssetDescription()
                {
                    SteamId = item.AssetDescription.ClassId,
                    Name = item.AssetDescription.MarketName,
                    BackgroundColour = item.AssetDescription.BackgroundColour.SteamColourToHexString(),
                    ForegroundColour = item.AssetDescription.NameColour.SteamColourToHexString(),
                    IconUrl = new SteamEconomyImageBlobRequest(item.AssetDescription.IconUrl).Uri.ToString(),
                    IconLargeUrl = new SteamEconomyImageBlobRequest(item.AssetDescription.IconUrlLarge).Uri.ToString(),
                    WorkshopFile = String.IsNullOrEmpty(workshopFileId) ? null : new SteamAssetWorkshopFile()
                    {
                        SteamId = workshopFileId
                    }
                }
            });

            return dbItem;
        }

        public static async Task UpdateSteamItemOrders(SteamDbContext db, Guid itemId, Guid currencyId, SteamMarketItemOrdersHistogramJsonRequest request)
        {
            var orders = await new SteamCommunityClient().GetMarketItemOrdersHistogram(request);
            if (orders?.Success != true)
            {
                return;
            }

            var item = db.SteamMarketItems.SingleOrDefault(x => x.Id == itemId);
            if (item == null)
            {
                return;
            }

            item.LastCheckedOn = DateTimeOffset.Now;
            item.CurrencyId = currencyId;
            item.RebuildOrders(
                ParseSteamItemOrdersFromGraph(orders.BuyOrderGraph),
                ParseSteamItemOrdersFromGraph(orders.SellOrderGraph)
            );
        }

        private static Models.Steam.SteamMarketItemOrder[] ParseSteamItemOrdersFromGraph(string[][] orderGraph)
        {
            var orders = new List<Models.Steam.SteamMarketItemOrder>();
            if (orderGraph == null)
            {
                return orders.ToArray();
            }

            var totalQuantity = 0;
            for (int i = 0; i < orderGraph.Length; i++)
            {
                var price = Int32.Parse(orderGraph[i][0].Replace(",", "").Replace(".", ""));
                var quantity = (Int32.Parse(orderGraph[i][1].Replace(",", "")) - totalQuantity);
                orders.Add(new Models.Steam.SteamMarketItemOrder()
                {
                    Price = price,
                    Quantity = quantity,
                });
                totalQuantity += quantity;
            }

            return orders.ToArray();
        }

        public static async Task<SteamMarketItem> UpdateSteamItemId(SteamDbContext db, SteamMarketItem item)
        {
            var itemNameId = await new SteamCommunityClient().GetMarketListingItemNameId(
                new SteamMarketListingPageRequest()
                {
                    AppId = item.App.SteamId,
                    MarketHashName = item.Description.Name,
                }
            );

            if (!String.IsNullOrEmpty(itemNameId))
            {
                item.SteamId = itemNameId;
                await db.SaveChangesAsync();
            }

            return item;
        }
    }
}
