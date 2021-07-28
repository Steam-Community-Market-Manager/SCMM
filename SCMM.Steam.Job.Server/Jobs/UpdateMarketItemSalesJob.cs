using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.API;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Job.Server.Jobs.Cron;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

            var language = db.SteamLanguages.FirstOrDefault(x => x.IsDefault);
            if (language == null)
            {
                return;
            }

            var currency = db.SteamCurrencies.FirstOrDefault(x => x.IsDefault);
            if (currency == null)
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
                            Language = language.SteamId,
                            CurrencyId = currency.SteamId,
                            NoRender = true
                        }
                    );

                    await steamService.UpdateMarketItemSalesHistory(
                        item,
                        currency.Id,
                        response
                    );
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
