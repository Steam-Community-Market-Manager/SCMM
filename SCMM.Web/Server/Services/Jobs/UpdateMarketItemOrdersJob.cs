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
using SCMM.Steam.Client;
using Microsoft.EntityFrameworkCore;

namespace SCMM.Web.Server.Services.Jobs
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
            using (var scope = _scopeFactory.CreateScope())
            {
                var commnityClient = scope.ServiceProvider.GetService<SteamCommunityClient>();
                var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

                var items = db.SteamMarketItems
                    .Where(x => !String.IsNullOrEmpty(x.SteamId))
                    .OrderBy(x => x.LastCheckedOrdersOn)
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

                foreach (var batch in items.Batch(100))
                {
                    _logger.LogInformation($"Updating market item orders (ids: {batch.Count()})");
                    var batchTasks = batch.Select(x =>
                        commnityClient.GetMarketItemOrdersHistogram(
                            new SteamMarketItemOrdersHistogramJsonRequest()
                            {
                                ItemNameId = x.SteamId,
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
                        await steamService.UpdateSteamMarketItemOrders(
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
