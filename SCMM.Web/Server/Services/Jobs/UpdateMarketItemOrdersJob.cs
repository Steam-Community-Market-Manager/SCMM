using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Shared.Community.Requests.Json;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class UpdateMarketItemOrdersJob : CronJobService
    {
        private readonly ILogger<UpdateMarketItemOrdersJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public UpdateMarketItemOrdersJob(IConfiguration configuration, ILogger<UpdateMarketItemOrdersJob> logger, IServiceScopeFactory scopeFactory)
            : base(configuration.GetJobConfiguration<UpdateMarketItemOrdersJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();

                var itemSteamIds = db.SteamMarketItems
                    .Where(x => !String.IsNullOrEmpty(x.SteamId))
                    .OrderBy(x => x.LastCheckedOn)
                    .Select(x => new
                    {
                        Id = x.Id,
                        SteamId = x.SteamId
                    })
                    .Take(100)
                    .ToList();

                if (!itemSteamIds.Any())
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

                foreach (var item in itemSteamIds)
                {
                    await SteamService.UpdateSteamItemOrders(
                        db,
                        item.Id,
                        currency.Id,
                        new SteamMarketItemOrdersHistogramJsonRequest()
                        {
                            ItemNameId = item.SteamId,
                            Language = language.SteamId,
                            CurrencyId = currency.SteamId,
                            NoRender = true
                        }
                    );

                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
