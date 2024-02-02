using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Web.Client;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Job.Server.Attributes;
using System.Collections.Concurrent;

namespace SCMM.Steam.Job.Server.Jobs;

[Job("Update-Market-Item-Activity", 30)]
public class UpdateMarketItemActivityJob : InvocableJob
{
    private readonly ILogger<UpdateMarketItemActivityJob> _logger;
    private readonly SteamDbContext _db;
    private readonly SteamCommunityWebClient _steamCommunityWebClient;
    private readonly IWebProxyManager _webProxyManager;

    // TODO: Make these configurable
    private const int MarketItemBatchSize = 300;
    private readonly TimeSpan MarketItemMinimumAgeSinceLastUpdate = TimeSpan.FromMinutes(7);

    public UpdateMarketItemActivityJob(ILogger<UpdateMarketItemActivityJob> logger, SteamDbContext db, SteamCommunityWebClient steamCommunityWebClient, IWebProxyManager webProxyManager)
    {
        _logger = logger;
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
        _webProxyManager = webProxyManager;
    }

    public override async Task Run(CancellationToken cancellationToken)
    {
        var jobId = Guid.NewGuid();

        // Check that there are enough web proxies available to handle this batch of SCM requests, otherwise we cannot run
        var availableProxies = _webProxyManager.GetAvailableProxyCount(new Uri(Constants.SteamCommunityUrl));
        if (availableProxies < MarketItemBatchSize)
        {
            throw new Exception($"Update of market item activity information cannot run as there are not enough available web proxies to handle the requests (proxies: {availableProxies}/{MarketItemBatchSize})");
        }

        _logger.LogTrace($"Updating market item activity information (id: {jobId})");

        // Find the next batch of items to be updated
        var cutoff = DateTimeOffset.Now.Subtract(MarketItemMinimumAgeSinceLastUpdate);
        var items = _db.SteamMarketItems
            .AsNoTracking()
            .Where(x => x.Description.NameId != null)
            .Where(x => x.Description.IsMarketable)
            .Where(x => x.LastCheckedActivityOn == null || x.LastCheckedActivityOn <= cutoff)
            .Where(x => x.App.FeatureFlags.HasFlag(SteamAppFeatureFlags.ItemMarketActivityTracking))
            .OrderBy(x => x.LastCheckedActivityOn)
            .Take(MarketItemBatchSize)
            .Select(x => new
            {
                x.Id,
                ItemNameId = x.SteamId,
                ItemDescriptionId = x.DescriptionId,
                x.CurrencyId,
                CurrencySteamId = x.Currency.SteamId,
                AppId = x.App.SteamId,
                MarketHashName = x.Description.NameHash
            })
            .ToArray();
        if (!items.Any())
        {
            return;
        }

        // Ignore Steam data which has not changed recently
        _steamCommunityWebClient.IfModifiedSinceTimeAgo = MarketItemMinimumAgeSinceLastUpdate;

        // Fetch item activity from steam in parallel (for better performance)
        var itemResponseMappings = new ConcurrentDictionary<Guid, SteamMarketItemOrdersActivityJsonResponse>();
        await Parallel.ForEachAsync(items, async (item, cancellationToken) =>
        {
            try
            {
                itemResponseMappings[item.Id] = await _steamCommunityWebClient.GetMarketItemOrdersActivityAsync(
                    new SteamMarketItemOrdersActivityJsonRequest()
                    {
                        ItemNameId = item.ItemNameId.ToString(),
                        Language = Constants.SteamDefaultLanguage,
                        CurrencyId = item.CurrencySteamId,
                        NoRender = true
                    },
                    item.AppId.ToString(),
                    item.MarketHashName
                );
            }
            catch (SteamRequestException ex)
            {
                _logger.LogError(ex, $"Failed to update market item activity for '{item.MarketHashName}' ({item.Id}). {ex.Message}");
            }
            catch (SteamNotModifiedException ex)
            {
                _logger.LogTrace(ex, $"No change in market item activity for '{item.MarketHashName}' ({item.Id}) since last request. {ex.Message}");
            }
        });

        // Parse the responses and update the item prices
        if (itemResponseMappings.Any())
        {
            foreach (var response in itemResponseMappings)
            {
                var item = items.FirstOrDefault(x => x.Id == response.Key);
                try
                {
                    if (item == null || response.Value?.Success != true || response.Value?.Activity == null)
                    {
                        continue;
                    }
                    foreach (var activity in response.Value.Activity)
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
                            DescriptionId = item.ItemDescriptionId,
                            ItemId = item.Id,
                            CurrencyId = item.CurrencyId,
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

                    await _db.SaveChangesAsync();

                    await _db.SteamMarketItems
                        .Where(x => x.Id == item.Id)
                        .ExecuteUpdateAsync(u => u.SetProperty(x => x.LastCheckedActivityOn, DateTimeOffset.UtcNow));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process and save market item activity for '{item.MarketHashName}' ({item.Id}). {ex.Message}");
                    continue;
                }
            }
        }

        _logger.LogTrace($"Updated {itemResponseMappings.Count} market item activity information (id: {jobId})");
    }
}
