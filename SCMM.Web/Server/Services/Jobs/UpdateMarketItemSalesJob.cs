using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Shared.Community.Requests.Json;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Extensions;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
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
            using (var scope = _scopeFactory.CreateScope())
            {
                var commnityClient = scope.ServiceProvider.GetService<SteamCommunityClient>();
                var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
                var db = scope.ServiceProvider.GetRequiredService<ScmmDbContext>();

                var items = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Description)
                    .OrderBy(x => x.LastCheckedSalesOn)
                    .Take(5) // batch 5 at a time
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

                foreach (var item in items)
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

                    try
                    {
                        await steamService.UpdateSteamMarketItemSalesHistory(
                            item,
                            currency.Id,
                            response
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to update market item sales history for '{item.SteamId}'");
                        continue;
                    }
                }

                db.SaveChanges();
            }
        }
    }
}
