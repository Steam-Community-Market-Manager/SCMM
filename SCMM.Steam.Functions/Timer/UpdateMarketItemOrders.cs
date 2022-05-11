using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Steam.API;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions.Timer;

public class UpdateMarketItemOrders
{
    private readonly SteamDbContext _db;
    private readonly SteamCommunityWebClient _steamCommunityWebClient;
    private readonly SteamService _steamService;

    public UpdateMarketItemOrders(SteamDbContext db, SteamCommunityWebClient steamCommunityWebClient, SteamService steamService)
    {
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
        _steamService = steamService;
    }

    [Function("Update-Market-Item-Orders")]
    public async Task Run([TimerTrigger("0 */2 * * * *")] /* every even minute */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Orders");

        var cutoff = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(1));
        var items = _db.SteamMarketItems
            .Include(x => x.Currency)
            .Where(x => !string.IsNullOrEmpty(x.SteamId))
            .Where(x => x.LastCheckedOrdersOn == null || x.LastCheckedOrdersOn <= cutoff)
            .OrderBy(x => x.LastCheckedOrdersOn)
            .Take(100) // batch 100 at a time
            .ToList();

        if (!items.Any())
        {
            return;
        }

        var id = Guid.NewGuid();
        logger.LogTrace($"Updating market item orders information (id: {id}, count: {items.Count()})");
        foreach (var item in items)
        {
            var response = await _steamCommunityWebClient.GetMarketItemOrdersHistogram(
                new SteamMarketItemOrdersHistogramJsonRequest()
                {
                    ItemNameId = item.SteamId,
                    Language = Constants.SteamDefaultLanguage,
                    CurrencyId = item.Currency.SteamId,
                    NoRender = true
                }
            );

            try
            {
                await _steamService.UpdateMarketItemOrders(item, response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item order history for '{item.SteamId}'. {ex.Message}");
                continue;
            }
        }

        _db.SaveChanges();
        logger.LogTrace($"Updated market item orders information (id: {id})");
    }
}
