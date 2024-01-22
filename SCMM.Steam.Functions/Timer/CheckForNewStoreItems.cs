using CommandQuery;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.API.Events;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.WebApi.Requests.ISteamEconomy;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using System.Globalization;

namespace SCMM.Steam.Functions.Timer;

public class CheckForNewStoreItems
{
    private readonly ICommandProcessor _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;
    private readonly SteamDbContext _steamDb;
    private readonly SteamWebApiClient _apiClient;
    private readonly IServiceBus _serviceBus;

    public CheckForNewStoreItems(ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, SteamDbContext steamDb, SteamWebApiClient apiClient, IServiceBus serviceBus)
    {
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;
        _steamDb = steamDb;
        _apiClient = apiClient;
        _serviceBus = serviceBus;
    }

    [Function("Check-New-Store-Items")]
    public async Task Run([TimerTrigger("0 * * * * *")] /* every minute */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Check-New-Store-Items");

        var apps = await _steamDb.SteamApps
            .Where(x => x.FeatureFlags.HasFlag(SteamAppFeatureFlags.ItemStorePriceTracking))
            .ToArrayAsync();

        foreach (var app in apps)
        {
            logger.LogTrace($"Checking for new item definition archives (appId: {app.SteamId})");

            // Get the latest asset description prices
            var assetPricesResponse = await _apiClient.SteamEconomyGetAssetPricesAsync(new GetAssetPricesJsonRequest()
            {
                AppId = uint.Parse(app.SteamId)
            });
            if (assetPricesResponse?.Success != true)
            {
                logger.LogError($"Failed to get store asset prices (appId: {app.SteamId})");
            }

            // Find the missing asset descriptions
            var storeAssetClassIds = assetPricesResponse.Assets
                .Where(x => !String.IsNullOrEmpty(x.ClassId))
                .Select(x => UInt64.Parse(x.ClassId))
                .ToArray();
            var existingAssetClassIds = await _steamDb.SteamStoreItems
                .Where(x => x.AppId == app.Id)
                .Where(x => x.Description.ClassId != null)
                .Where(x => storeAssetClassIds.Contains(x.Description.ClassId.Value))
                .Select(x => x.Description.ClassId)
                .ToArrayAsync();
            var missingAssetClassIds = storeAssetClassIds
                .Where(x => !existingAssetClassIds.Contains(x))
                .ToArray();

            if (missingAssetClassIds.Any())
            {
                // Prices are returned in USD by default
                var usdCurrency = _steamDb.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
                if (usdCurrency == null)
                {
                    return;
                }

                // Import any missing asset descriptions
                foreach (var assetClassId in missingAssetClassIds)
                {
                    var assetPrice = assetPricesResponse.Assets.FirstOrDefault(x => x.ClassId == assetClassId.ToString());
                    if (assetPrice == null)
                    {
                        continue;
                    }

                    // Import the asset description
                    var importedAssetDescription = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionRequest()
                    {
                        AppId = UInt64.Parse(app.SteamId),
                        AssetClassId = assetClassId,
                    });

                    // If the asset description is not yet accepted, accept it now
                    var assetDescription = importedAssetDescription.AssetDescription;
                    assetDescription.IsAccepted = true;
                    if (assetDescription.TimeAccepted == null)
                    {
                        if (!String.IsNullOrEmpty(assetPrice.Date))
                        {
                            DateTimeOffset storeDate;
                            if (DateTimeOffset.TryParseExact(assetPrice.Date, "yyyy-M-d", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out storeDate) ||
                                DateTimeOffset.TryParseExact(assetPrice.Date, "yyyy/M/d", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out storeDate))
                            {
                                assetDescription.TimeAccepted = storeDate;
                            }
                        }
                        else
                        {
                            assetDescription.TimeAccepted = DateTimeOffset.UtcNow;
                        }
                    }

                    // Create the store item
                    var storeItem = new SteamStoreItem()
                    {
                        App = app,
                        AppId = app.Id,
                        Description = assetDescription,
                        DescriptionId = assetDescription.Id
                    };

                    // Set the store item price
                    var prices = assetPrice.Prices;
                    storeItem.UpdatePrice(
                        usdCurrency,
                        prices.FirstOrDefault(x => x.Key == usdCurrency?.Name).Value,
                        new PersistablePriceDictionary(prices)
                    );

                    // Mark the store item as available
                    storeItem.SteamId = assetPrice.Name;
                    storeItem.IsAvailable = true;

                    app.StoreItems.Add(storeItem);
                    await _steamDb.SaveChangesAsync();

                    await _serviceBus.SendMessageAsync(new StoreItemAddedMessage()
                    {
                        AppId = UInt64.Parse(app.SteamId),
                        AppName = app.Name,
                        AppIconUrl = app.IconUrl,
                        AppColour = app.PrimaryColor,
                        StoreId = null,
                        StoreName = null,
                        CreatorId = storeItem.Description?.CreatorId,
                        CreatorName = storeItem.Description?.CreatorProfile?.Name,
                        CreatorAvatarUrl = storeItem.Description?.CreatorProfile?.AvatarUrl,
                        ItemId = UInt64.Parse(storeItem.SteamId),
                        ItemType = storeItem.Description?.ItemType,
                        ItemShortName = storeItem.Description?.ItemShortName,
                        ItemName = storeItem.Description?.Name,
                        ItemDescription = storeItem.Description?.Description,
                        ItemCollection = storeItem.Description?.ItemCollection,
                        ItemIconUrl = storeItem.Description?.IconUrl ?? storeItem.Description?.IconLargeUrl,
                        ItemImageUrl = storeItem.Description?.PreviewUrl ?? storeItem.Description?.IconLargeUrl ?? storeItem.Description?.IconUrl,
                        ItemPrices = storeItem.Prices.Select(x => new StoreItemAddedMessage.Price()
                        {
                            Currency = x.Key,
                            Value = x.Value,
                            Description = usdCurrency?.ToPriceString(x.Value)
                        }).ToArray()
                    });
                }
            }
        }
    }
}
