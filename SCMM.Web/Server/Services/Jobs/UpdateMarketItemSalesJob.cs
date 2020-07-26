using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Shared;
using SCMM.Steam.Shared.Community.Requests.Json;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Services.Jobs.CronJob;
using SCMM.Web.Shared;
using System;
using System.Linq;
using System.Reactive.Linq;
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
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

                var items = db.SteamMarketItems
                    .Include(x => x.App)
                    .Include(x => x.Description)
                    .OrderBy(x => x.LastCheckedSalesOn)
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

                foreach (var batch in items.Batch(100))
                {
                    _logger.LogInformation($"Updating market item sales history (ids: {batch.Count()})");
                    var batchTasks = batch.Select(x =>
                        commnityClient.GetMarketPriceHistory(
                            new SteamMarketPriceHistoryJsonRequest()
                            {
                                AppId = x.App.SteamId,
                                MarketHashName = x.Description.Name,
                                Language = language.SteamId,
                                CurrencyId = currency.SteamId,
                                NoRender = true
                            }
                        )
                        .ContinueWith(t => new
                        {
                            Item = x,
                            Response = t.Result
                        })
                    ).ToArray();

                    Task.WaitAll(batchTasks);
                    foreach (var task in batchTasks)
                    {
                        try
                        {
                            await steamService.UpdateSteamMarketItemSalesHistory(
                                task.Result.Item,
                                currency.Id,
                                task.Result.Response
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to update market item sales history for '{task.Result.Item.SteamId}'");
                            continue;
                        }
                    }

                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
