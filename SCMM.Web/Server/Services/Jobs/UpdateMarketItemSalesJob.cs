using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Shared.Community.Requests.Json;
using SCMM.Web.Shared;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using SCMM.Steam.Client;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Shared;

namespace SCMM.Web.Server.Services.Jobs
{
    public class UpdateMarketItemSalesJob : CronJobService
    {
        private readonly ILogger<UpdateMarketItemSalesJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SteamConfiguration _steamConfiguration;

        public UpdateMarketItemSalesJob(IConfiguration configuration, ILogger<UpdateMarketItemSalesJob> logger, IServiceScopeFactory scopeFactory)
            : base(configuration.GetJobConfiguration<UpdateMarketItemSalesJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _steamConfiguration = configuration.GetSteamConfiguration();
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
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

                var language = db.SteamLanguages.FirstOrDefault(x => x.Id.ToString() == Constants.DefaultLanguageId);
                if (language == null)
                {
                    return;
                }

                var currency = db.SteamCurrencies.FirstOrDefault(x => x.Id.ToString() == Constants.DefaultCurrencyId);
                if (currency == null)
                {
                    return;
                }

                var client = new SteamCommunityClient(_steamConfiguration.GetCookieContainer());
                foreach (var batch in items.Batch(100))
                {
                    var batchTasks = batch.Select(x =>
                        client.GetMarketPriceHistory(
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
                            Response  = t.Result
                        })
                    ).ToArray();

                    Task.WaitAll(batchTasks);
                    foreach (var task in batchTasks)
                    {
                        await steamService.UpdateSteamMarketItemSalesHistory(
                            task.Result.Item,
                            currency.Id,
                            task.Result.Response
                        );
                    }

                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
