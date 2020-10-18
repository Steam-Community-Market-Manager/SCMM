﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Steam.Client;
using SCMM.Steam.Shared;
using SCMM.Steam.Shared.Community.Models;
using SCMM.Steam.Shared.Community.Requests.Blob;
using SCMM.Steam.Shared.Community.Requests.Html;
using SCMM.Steam.Shared.Community.Requests.Json;
using SCMM.Steam.Shared.Community.Responses.Json;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Steam;
using SCMM.Web.Server.Data.Types;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Shared.Domain.DTOs.InventoryItems;
using SCMM.Web.Shared;
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

namespace SCMM.Web.Server.Services
{
    public class SteamService
    {
        private readonly SteamDbContext _db;
        private readonly SteamConfiguration _cfg;
        private readonly SteamCommunityClient _communityClient;
        private readonly SteamCurrencyService _currencyService;
        private readonly SteamLanguageService _languageService;

        public SteamService(SteamDbContext db, IConfiguration cfg, SteamCommunityClient communityClient, SteamCurrencyService currencyService, SteamLanguageService languageService)
        {
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _communityClient = communityClient;
            _currencyService = currencyService;
            _languageService = languageService;
        }

        public async Task<byte[]> GetImage(string url)
        {
            return await _communityClient.GetImage(new SteamBlobRequest(url));
        }

        public DateTimeOffset GetStoreNextUpdateExpectedOn()
        {
            var nextStoreUpdateUtc = _db.SteamAssetWorkshopFiles
                .Where(x => x.AcceptedOn != null)
                .Select(x => x.AcceptedOn.Value)
                .Max().UtcDateTime;

            // Store normally updates every thursday or friday around 9pm (UK time)
            nextStoreUpdateUtc = (nextStoreUpdateUtc.Date + new TimeSpan(21, 0, 0));
            do
            {
                nextStoreUpdateUtc = nextStoreUpdateUtc.AddDays(1);
            } while (nextStoreUpdateUtc.DayOfWeek != DayOfWeek.Thursday);

            // If the expected store date is still in the past, assume it is a day late
            // NOTE: Has a tolerance of 3hrs from the expected time
            while ((nextStoreUpdateUtc + TimeSpan.FromHours(3)) <= DateTime.UtcNow)
            {
                nextStoreUpdateUtc = nextStoreUpdateUtc.AddDays(1);
            }

            return new DateTimeOffset(nextStoreUpdateUtc, TimeZoneInfo.Utc.BaseUtcOffset);
        }

        public ProfileInventoryTotalsDTO GetProfileInventoryTotal(string steamId, string currencyName)
        {
            var currency = _currencyService.GetByNameOrDefault(currencyName);
            var profileInventoryItems = _db.SteamInventoryItems
                .Where(x => x.Owner.SteamId == steamId || x.Owner.ProfileId == steamId)
                .Where(x => x.MarketItemId != null)
                .Select(x => new
                {
                    Quantity = x.Quantity,
                    BuyPrice = x.BuyPrice,
                    ExchangeRateMultiplier = x.Currency.ExchangeRateMultiplier,
                    MarketItemLast1hrValue = x.MarketItem.Last1hrValue,
                    MarketItemLast24hrValue = x.MarketItem.Last24hrValue,
                    MarketItemResellPrice = x.MarketItem.ResellPrice,
                    MarketItemResellTax = x.MarketItem.ResellTax,
                    MarketItemExchangeRateMultiplier = x.MarketItem.Currency.ExchangeRateMultiplier
                })
                .ToList();

            var profileInventory = new
            {
                TotalItems = profileInventoryItems
                    .Sum(x => x.Quantity),
                TotalInvested = profileInventoryItems
                    .Where(x => x.BuyPrice != null && x.BuyPrice != 0 && x.ExchangeRateMultiplier != 0)
                    .Sum(x => (x.BuyPrice / x.ExchangeRateMultiplier) * x.Quantity),
                TotalMarketValueLast1hr = profileInventoryItems
                    .Where(x => x.MarketItemLast1hrValue != 0 && x.MarketItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.MarketItemLast1hrValue / x.MarketItemExchangeRateMultiplier) * x.Quantity),
                TotalMarketValueLast24hr = profileInventoryItems
                    .Where(x => x.MarketItemLast24hrValue != 0 && x.MarketItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.MarketItemLast24hrValue / x.MarketItemExchangeRateMultiplier) * x.Quantity),
                TotalResellValue = profileInventoryItems
                    .Where(x => x.MarketItemResellPrice != 0 && x.MarketItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.MarketItemResellPrice / x.MarketItemExchangeRateMultiplier) * x.Quantity),
                TotalResellTax = profileInventoryItems
                    .Where(x => x.MarketItemResellTax != 0 && x.MarketItemExchangeRateMultiplier != 0)
                    .Sum(x => (x.MarketItemResellTax / x.MarketItemExchangeRateMultiplier) * x.Quantity)
            };

            return new ProfileInventoryTotalsDTO()
            {
                TotalItems = profileInventory.TotalItems,
                TotalInvested = currency.CalculateExchange(profileInventory.TotalInvested ?? 0),
                TotalMarketValue = currency.CalculateExchange(profileInventory.TotalMarketValueLast1hr),
                TotalMarket24hrMovement = currency.CalculateExchange(profileInventory.TotalMarketValueLast1hr - profileInventory.TotalMarketValueLast24hr),
                TotalResellValue = currency.CalculateExchange(profileInventory.TotalResellValue),
                TotalResellTax = currency.CalculateExchange(profileInventory.TotalResellTax),
                TotalResellProfit = (
                    currency.CalculateExchange(profileInventory.TotalResellValue - profileInventory.TotalResellTax) - currency.CalculateExchange(profileInventory.TotalInvested ?? 0)
                )
            };
        }

        public async Task<SteamProfile> AddOrUpdateSteamProfile(string steamId, bool fetchLatest = false)
        {
            if (string.IsNullOrEmpty(steamId))
            {
                return null;
            }

            var profile = await _db.SteamProfiles.FirstOrDefaultAsync(
                x => x.SteamId == steamId || x.ProfileId == steamId
            );
            if (profile != null && !fetchLatest)
            {
                // Nothing to update
                return profile;
            }

            // Is this a int64 steam id?
            if (Int64.TryParse(steamId, out _))
            {
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
                    profileId = (Regex.Match(profileId, SteamConstants.SteamProfileIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? profileId);
                }
                if (String.IsNullOrEmpty(profileId))
                {
                    profileId = (Regex.Match(profileId, SteamConstants.SteamProfileIdRegex).Groups.OfType<Capture>().LastOrDefault()?.Value ?? profileId);
                }

                profile = profile ?? new SteamProfile()
                {
                    SteamId = steamId,
                    ProfileId = profileId
                };

                profile.Name = response.Data.Nickname?.Trim();
                profile.AvatarUrl = response.Data.AvatarMediumUrl;
                profile.AvatarLargeUrl = response.Data.AvatarFullUrl;
                profile.Country = response.Data.CountryCode;
            }

            // Else, it is probably a string profile id...
            else
            {
                var profileId = steamId;
                var response = await _communityClient.GetProfile(new SteamProfilePageRequest()
                {
                    ProfileId = profileId,
                    Xml = true
                });
                if (response == null)
                {
                    return null;
                }

                profile = profile ?? new SteamProfile()
                {
                    SteamId = response.SteamID64.ToString(),
                    ProfileId = profileId
                };

                profile.Name = response.SteamID?.Trim();
                profile.AvatarUrl = response.AvatarMedium;
                profile.AvatarLargeUrl = response.AvatarFull;
                profile.Country = response.Location;
            }

            if (profile.Id == Guid.Empty)
            {
                _db.SteamProfiles.Add(profile);
            }

            await _db.SaveChangesAsync();
            return profile;
        }

        public async Task<SteamProfile> FetchProfileInventory(string steamId)
        {
            var profile = await _db.SteamProfiles
                .Include(x => x.InventoryItems).ThenInclude(x => x.App)
                .Include(x => x.InventoryItems).ThenInclude(x => x.Description)
                .Include(x => x.InventoryItems).ThenInclude(x => x.Currency)
                .Include(x => x.InventoryItems).ThenInclude(x => x.MarketItem)
                .Include(x => x.InventoryItems).ThenInclude(x => x.MarketItem.Description)
                .Include(x => x.InventoryItems).ThenInclude(x => x.MarketItem.Currency)
                .FirstOrDefaultAsync(x => x.SteamId == steamId || x.ProfileId == steamId);
            if (profile == null)
            {
                profile = await AddOrUpdateSteamProfile(steamId);
                if (profile == null)
                {
                    return null;
                }
            }

            var language = _db.SteamLanguages
                .FirstOrDefault(x => x.IsDefault);
            if (language == null)
            {
                return null;
            }

            var apps = _db.SteamApps.ToList();
            if (!apps.Any())
            {
                return profile;
            }

            foreach (var app in apps)
            {
                // Fetch assets
                var inventory = await _communityClient.GetInventoryPaginated(new SteamInventoryPaginatedJsonRequest()
                {
                    AppId = app.SteamId,
                    SteamId = profile.SteamId,
                    Start = 1,
                    Count = SteamInventoryPaginatedJsonRequest.MaxPageSize,
                    NoRender = true
                });
                if (inventory?.Success != true)
                {
                    continue;
                }

                // Add assets
                var missingAssets = inventory.Assets
                    .Where(x => !profile.InventoryItems.Any(y => y.SteamId == x.AssetId))
                    .ToList();
                foreach (var asset in missingAssets)
                {
                    var assetDescription = await AddOrUpdateAssetDescription(app, language, UInt64.Parse(asset.ClassId));
                    if (assetDescription == null)
                    {
                        continue;
                    }
                    var marketItem = await _db.SteamMarketItems
                        .Include(x => x.Currency)
                        .FirstOrDefaultAsync(x => x.Description.SteamId == asset.ClassId);
                    var inventoryItem = new SteamInventoryItem()
                    {
                        SteamId = asset.AssetId,
                        Owner = profile,
                        OwnerId = profile.Id,
                        App = app,
                        AppId = app.Id,
                        Description = assetDescription,
                        DescriptionId = assetDescription.Id,
                        MarketItem = marketItem,
                        MarketItemId = marketItem?.Id,
                        Quantity = asset.Amount
                    };

                    profile.InventoryItems.Add(inventoryItem);
                }

                // Update assets
                foreach (var asset in inventory.Assets)
                {
                    var existingAsset = profile.InventoryItems.FirstOrDefault(x => x.SteamId == asset.AssetId);
                    if (existingAsset != null)
                    {
                        existingAsset.Quantity = asset.Amount;
                        if (existingAsset.MarketItemId == null)
                        {
                            var marketItem = await _db.SteamMarketItems
                                .Include(x => x.Currency)
                                .FirstOrDefaultAsync(x => x.Description.SteamId == asset.ClassId);
                            if (marketItem != null)
                            {
                                existingAsset.MarketItem = marketItem;
                                existingAsset.MarketItemId = marketItem.Id;
                            }
                        }
                    }
                }

                // Remove assets
                var removedAssets = profile.InventoryItems
                    .Where(x => !inventory.Assets.Any(y => y.AssetId == x.SteamId))
                    .ToList();
                foreach (var asset in removedAssets)
                {
                    profile.InventoryItems.Remove(asset);
                }

                profile.LastUpdatedInventoryOn = DateTimeOffset.Now;
                await _db.SaveChangesAsync();
            }

            return profile;
        }

        public async Task<Data.Models.Steam.SteamAssetFilter> AddOrUpdateAppAssetFilter(SteamApp app, Steam.Shared.Community.Models.SteamAssetFilter filter)
        {
            var existingFilter = app.Filters.FirstOrDefault(x => x.SteamId == filter.Name);
            if (existingFilter != null)
            {
                // Nothing to update
                return existingFilter;
            }

            var newFilter = new Data.Models.Steam.SteamAssetFilter()
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

        public Data.Models.Steam.SteamAssetDescription UpdateAssetDescription(Data.Models.Steam.SteamAssetDescription assetDescription, AssetClassInfoModel assetClass)
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

        public async Task<Data.Models.Steam.SteamAssetDescription> UpdateAssetDescription(Data.Models.Steam.SteamAssetDescription assetDescription, PublishedFileDetailsModel publishedFile, bool updateSubscriptionGraph = false)
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
                        if (workshopFile.AcceptedOn.HasValue)
                        {
                            var culture = CultureInfo.InvariantCulture;
                            var acceptedOn = workshopFile.AcceptedOn.Value.UtcDateTime;
                            int acceptedOnWeek = culture.Calendar.GetWeekOfYear(acceptedOn, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);
                            assetDescription.Tags[SteamConstants.SteamAssetTagAcceptedYear] = acceptedOn.ToString("yyyy");
                            assetDescription.Tags[SteamConstants.SteamAssetTagAcceptedWeek] = $"Week {acceptedOnWeek}";
                        }
                    }
                }

                workshopFile.Subscriptions = (int)Math.Max(publishedFile.LifetimeSubscriptions, publishedFile.Subscriptions);
                if (updateSubscriptionGraph)
                {
                    var utcDate = DateTime.UtcNow.Date;
                    var maxSubscriptions = workshopFile.Subscriptions;
                    if (workshopFile.SubscriptionsGraph.ContainsKey(utcDate))
                    {
                        maxSubscriptions = (int)Math.Max(maxSubscriptions, workshopFile.SubscriptionsGraph[utcDate]);
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

        public SteamStoreItem UpdateStoreItemRank(SteamStoreItem storeItem, int storeRankPosition, int storeRankTotal)
        {
            var utcDate = DateTime.UtcNow.Date;
            storeItem.StoreRankPosition = storeRankPosition;
            storeItem.StoreRankTotal = storeRankTotal;
            storeItem.StoreRankGraph[utcDate] = storeRankPosition;
            storeItem.StoreRankGraph = new Data.Types.PersistableGraphDataSet(
                storeItem.StoreRankGraph
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
            await _db.SaveChangesAsync();
            return dbWorkshopFile;
        }

        public async Task<Data.Models.Steam.SteamAssetDescription> AddOrUpdateAssetDescription(SteamApp app, SteamLanguage language, ulong classId)
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

            dbAssetDescription = new Data.Models.Steam.SteamAssetDescription()
            {
                SteamId = assetDescription.ClassId.ToString(),
                AppId = app.Id,
                Name = assetDescription.MarketName,
                BackgroundColour = assetDescription.BackgroundColor.SteamColourToHexString(),
                ForegroundColour = assetDescription.NameColor.SteamColourToHexString(),
                IconUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrl).Uri.ToString(),
                IconLargeUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrlLarge ?? assetDescription.IconUrl).Uri.ToString(),
                WorkshopFile = workshopFile,
                Tags = new Data.Types.PersistableStringDictionary(tags)
            };

            _db.SteamAssetDescriptions.Add(dbAssetDescription);
            await _db.SaveChangesAsync();
            return dbAssetDescription;
        }

        public async Task<Data.Models.Steam.SteamAssetDescription> AddOrUpdateAssetDescription(SteamApp app, Steam.Shared.Community.Models.SteamAssetDescription assetDescription)
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
            var viewWorkshopAction = assetDescription?.Actions?.FirstOrDefault(x => x.Name == SteamConstants.SteamActionViewWorkshopItem);
            if (viewWorkshopAction != null)
            {
                var workshopFileIdGroups = Regex.Match(viewWorkshopAction.Link, SteamConstants.SteamActionViewWorkshopItemRegex).Groups;
                workshopFileId = (workshopFileIdGroups.Count > 1) ? workshopFileIdGroups[1].Value : "0";
                workshopFile = await AddOrUpdateAssetWorkshopFile(app, workshopFileId);
            }

            dbAssetDescription = new Data.Models.Steam.SteamAssetDescription()
            {
                SteamId = assetDescription.ClassId.ToString(),
                AppId = app.Id,
                Name = assetDescription.MarketName,
                BackgroundColour = assetDescription.BackgroundColor.SteamColourToHexString(),
                ForegroundColour = assetDescription.NameColor.SteamColourToHexString(),
                IconUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrl).Uri.ToString(),
                IconLargeUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrlLarge ?? assetDescription.IconUrl).Uri.ToString(),
                WorkshopFile = workshopFile,
                Tags = new Data.Types.PersistableStringDictionary(tags)
            };

            _db.SteamAssetDescriptions.Add(dbAssetDescription);
            await _db.SaveChangesAsync();
            return dbAssetDescription;
        }

        public async Task<SteamStoreItem> AddOrUpdateAppStoreItem(SteamApp app, SteamLanguage language, AssetModel asset, DateTime timeChecked)
        {
            var dbItem = await _db.SteamStoreItems
                .Include(x => x.Description)
                .Where(x => x.AppId == app.Id)
                .FirstOrDefaultAsync(x => x.SteamId == asset.Name);

            if (dbItem != null)
            {
                // Update prices
                if (asset.Prices != null)
                {
                    dbItem.StorePrices = new PersistablePriceDictionary(GetPriceTable(asset.Prices));
                }
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

            app.StoreItems.Add(dbItem = new SteamStoreItem()
            {
                SteamId = asset.Name,
                AppId = app.Id,
                Description = assetDescription,
                StorePrices = new PersistablePriceDictionary(GetPriceTable(asset.Prices))
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
                SteamEconomyHelper.GetQuantityValueAsInt(histogram.BuyOrderCount),
                ParseSteamMarketItemOrdersFromGraph<SteamMarketItemSellOrder>(histogram.SellOrderGraph),
                SteamEconomyHelper.GetQuantityValueAsInt(histogram.SellOrderCount)
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
            where T : Data.Models.Steam.SteamMarketItemOrder, new()
        {
            var orders = new List<T>();
            if (orderGraph == null)
            {
                return orders.ToArray();
            }

            var totalQuantity = 0;
            for (int i = 0; i < orderGraph.Length; i++)
            {
                var price = SteamEconomyHelper.GetPriceValueAsInt(orderGraph[i][0]);
                var quantity = (SteamEconomyHelper.GetQuantityValueAsInt(orderGraph[i][1]) - totalQuantity);
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
                var price = SteamEconomyHelper.GetPriceValueAsInt(salesGraph[i][1]);
                var quantity = SteamEconomyHelper.GetQuantityValueAsInt(salesGraph[i][2]);
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
