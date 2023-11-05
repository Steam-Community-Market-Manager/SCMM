using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemActivity
{
    private readonly SteamDbContext _db;
    private readonly SteamCommunityWebClient _steamCommunityWebClient;

    public UpdateMarketItemActivity(SteamDbContext db, SteamCommunityWebClient steamCommunityWebClient)
    {
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
    }

    // TODO: This needs to be more efficent, too spammy
    //[Function("Update-Market-Item-Activity")]
    public async Task Run([TimerTrigger("0 0/5 * * * *")] /* every 5 minutes */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Activity");

        // Delete all market activity older than 7 days
        var cutoffData = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(7));
        var expiredActivity = await _db.SteamMarketItemActivity
            .Where(x => x.Timestamp < cutoffData)
            .ToListAsync();
        if (expiredActivity.Any())
        {
            foreach (var batch in expiredActivity.Batch(100))
            {
                _db.SteamMarketItemActivity.RemoveRange(batch);
                _db.SaveChanges();
            }
        }

        var assetDescriptions = await _db.SteamAssetDescriptions
            .Where(x => x.NameId != null && x.MarketItem != null)
            .Where(x => x.MarketItem.Last24hrSales > 1)
            .Select(x => new
            {
                AppId = x.App.SteamId,
                x.Id,
                x.NameId,
                x.NameHash,
                MarketItemId = x.MarketItem.Id,
                x.MarketItem.Last1hrSales
            })
            .OrderByDescending(x => x.Last1hrSales)
            .Take(1000)
            .ToListAsync();

        if (!assetDescriptions.Any())
        {
            return;
        }

        var language = _db.SteamLanguages.FirstOrDefault(x => x.Name == Constants.SteamLanguageEnglish);
        if (language == null)
        {
            return;
        }

        var usdCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
        if (usdCurrency == null)
        {
            return;
        }

        var id = Guid.NewGuid();
        logger.LogTrace($"Updating market item activity information (id: {id}, count: {assetDescriptions.Count()})");
        foreach (var assetDescription in assetDescriptions)
        {
            var progress = assetDescriptions.IndexOf(assetDescription);
            var total = assetDescriptions.Count;
            try
            {
                var response = await _steamCommunityWebClient.GetMarketItemOrdersActivityAsync(
                    new SteamMarketItemOrdersActivityJsonRequest()
                    {
                        ItemNameId = assetDescription.NameId.ToString(),
                        Language = language.SteamId,
                        CurrencyId = usdCurrency.SteamId,
                        NoRender = true
                    },
                    assetDescription.AppId,
                    assetDescription.NameHash
                );

                if (response?.Success != true || response?.Activity?.Any() != true)
                {
                    continue;
                }

                foreach (var activity in response.Activity)
                {
                    var activityType = SteamMarketItemActivityType.Other;
                    switch (activity.Type)
                    {
                        case "SellOrder": activityType = SteamMarketItemActivityType.CreatedSellOrder; break;
                        case "SellOrderMulti": activityType = SteamMarketItemActivityType.CreatedSellOrder; break;
                        case "SellOrderCancel": activityType = SteamMarketItemActivityType.CancelledSellOrder; break;
                        case "BuyOrder": activityType = SteamMarketItemActivityType.CreatedBuyOrder; break;
                        case "BuyOrderMulti": activityType = SteamMarketItemActivityType.CreatedBuyOrder; break;
                        case "BuyOrderCancel": activityType = SteamMarketItemActivityType.CancelledBuyOrder; break;
                        default: activityType = SteamMarketItemActivityType.Other; break;
                    }
                    var newActivity = new SteamMarketItemActivity()
                    {
                        Timestamp = activity.Time.SteamTimestampToDateTimeOffset(),
                        DescriptionId = assetDescription.Id,
                        ItemId = assetDescription.MarketItemId,
                        CurrencyId = usdCurrency.Id,
                        Type = activityType,
                        Price = activity.Price,
                        Quantity = activity.Quantity,
                        SellerName = activity.PersonaSeller,
                        SellerAvatarUrl = activity.AvatarSeller,
                        BuyerName = activity.PersonaBuyer,
                        BuyerAvatarUrl = activity.AvatarBuyer
                    };

                    var activityAlreadyExists = _db.SteamMarketItemActivity.Any(x =>
                        x.Timestamp == newActivity.Timestamp &&
                        x.DescriptionId == newActivity.DescriptionId &&
                        x.Type == newActivity.Type &&
                        x.Price == newActivity.Price &&
                        x.Quantity == newActivity.Quantity &&
                        x.SellerName == newActivity.SellerName &&
                        x.BuyerName == newActivity.BuyerName
                    );

                    if (!activityAlreadyExists)
                    {
                        _db.SteamMarketItemActivity.Add(newActivity);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item activity for '{assetDescription.NameId}'. {ex.Message}");
            }

            _db.SaveChanges();
        }

        logger.LogTrace($"Updated market item activity information (id: {id})");
    }
}
