using Microsoft.EntityFrameworkCore;
using SCMM.Steam.API;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Job.Server.Jobs.Cron;

namespace SCMM.Steam.Job.Server.Jobs
{
    public class UpdateMarketItemOrdersJob : CronJobService
    {
        private readonly ILogger<UpdateMarketItemOrdersJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public UpdateMarketItemOrdersJob(IConfiguration configuration, ILogger<UpdateMarketItemOrdersJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<UpdateMarketItemOrdersJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
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
            _logger.LogInformation($"Updating market item orders information (id: {id}, count: {items.Count()})");
            foreach (var item in items)
            {
                var response = await commnityClient.GetMarketItemOrdersHistogram(
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
                    await steamService.UpdateMarketItemOrders(item, response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to update market item order history for '{item.SteamId}'. {ex.Message}");
                    continue;
                }
            }

            db.SaveChanges();
            _logger.LogInformation($"Updated market item orders information (id: {id})");
        }
    }
}
