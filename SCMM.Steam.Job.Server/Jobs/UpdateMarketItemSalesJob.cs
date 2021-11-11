using Microsoft.EntityFrameworkCore;
using SCMM.Steam.API;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Job.Server.Jobs.Cron;

namespace SCMM.Steam.Job.Server.Jobs
{
    public class UpdateMarketItemSalesJob : CronJobService
    {
        private readonly ILogger<UpdateMarketItemSalesJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SteamConfiguration _steamConfiguration;

        public UpdateMarketItemSalesJob(IConfiguration configuration, ILogger<UpdateMarketItemSalesJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<UpdateMarketItemSalesJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _steamConfiguration = configuration.GetSteamConfiguration();
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var commnityClient = scope.ServiceProvider.GetService<SteamCommunityWebClient>();
            var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
            var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

            var cutoff = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(1));
            var items = db.SteamMarketItems
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

            var nzdCurrency = db.SteamCurrencies.FirstOrDefault(x => x.Name == "NZD");
            if (nzdCurrency == null)
            {
                return;
            }

            var id = Guid.NewGuid();
            _logger.LogInformation($"Updating market item sales information (id: {id}, count: {items.Count()})");
            foreach (var item in items)
            {
                try
                {
                    var response = await commnityClient.GetMarketPriceHistory(
                        new SteamMarketPriceHistoryJsonRequest()
                        {
                            AppId = item.App.SteamId,
                            MarketHashName = item.Description.Name,
                            //CurrencyId = item.Currency.SteamId
                        }
                    );

                    // HACK: Our Steam account is locked to NZD, we must convert all prices to the items currency
                    // TODO: Find/buy a Steam account that is locked to USD for better accuracy
                    await steamService.UpdateMarketItemSalesHistory(item, response, nzdCurrency);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to update market item sales history for '{item.SteamId}'. {ex.Message}");
                    continue;
                }
            }

            db.SaveChanges();
            _logger.LogInformation($"Updated market item sales information (id: {id})");
        }
    }
}
