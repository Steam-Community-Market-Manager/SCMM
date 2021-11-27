using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCMM.Steam.API;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions.Jobs;

public class UpdateMarketItemSalesJob
{
    private readonly SteamDbContext _db;
    private readonly SteamCommunityWebClient _steamCommunityWebClient;
    private readonly SteamService _steamService;

    public UpdateMarketItemSalesJob(SteamDbContext db, SteamCommunityWebClient steamCommunityWebClient, SteamService steamService)
    {
        _db = db;
        _steamCommunityWebClient = steamCommunityWebClient;
        _steamService = steamService;
    }

    [Function("Update-Market-Item-Sales")]
    public async Task Run([TimerTrigger("0 1-59/2 * * * *")] /* every odd minute */ object timer, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Market-Item-Sales");

        var cutoff = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(1));
        var items = _db.SteamMarketItems
            .Include(x => x.Currency)
            .Where(x => x.LastCheckedSalesOn == null || x.LastCheckedSalesOn <= cutoff)
            .OrderBy(x => x.LastCheckedSalesOn)
            .Include(x => x.App)
            .Include(x => x.Description)
            .Take(100) // batch 100 at a time
            .ToList();

        if (!items.Any())
        {
            return;
        }

        var nzdCurrency = _db.SteamCurrencies.FirstOrDefault(x => x.Name == "NZD");
        if (nzdCurrency == null)
        {
            return;
        }

        var id = Guid.NewGuid();
        logger.LogInformation($"Updating market item sales information (id: {id}, count: {items.Count()})");
        foreach (var item in items)
        {
            try
            {
                var response = await _steamCommunityWebClient.GetMarketPriceHistory(
                    new SteamMarketPriceHistoryJsonRequest()
                    {
                        AppId = item.App.SteamId,
                        MarketHashName = item.Description.Name,
                            //CurrencyId = item.Currency.SteamId
                        }
                );

                // HACK: Our Steam account is locked to NZD, we must convert all prices to the items currency
                // TODO: Find/buy a Steam account that is locked to USD for better accuracy
                await _steamService.UpdateMarketItemSalesHistory(item, response, nzdCurrency);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to update market item sales history for '{item.SteamId}'. {ex.Message}");
                continue;
            }
        }

        _db.SaveChanges();
        logger.LogInformation($"Updated market item sales information (id: {id})");
    }
}
