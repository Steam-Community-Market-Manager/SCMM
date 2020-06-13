using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Shared;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Services.Jobs.CronJob;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class CheckForNewStoreItemsJob : CronJobService
    {
        private readonly ILogger<CheckForNewStoreItemsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SteamConfiguration _steamConfiguration;

        public CheckForNewStoreItemsJob(IConfiguration configuration, ILogger<CheckForNewStoreItemsJob> logger, IServiceScopeFactory scopeFactory)
            : base(configuration.GetJobConfiguration<CheckForNewStoreItemsJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _steamConfiguration = configuration.GetSteamConfiguration();
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
                var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_steamConfiguration.ApplicationKey);
                var steamEconomy = steamWebInterfaceFactory.CreateSteamWebInterface<SteamEconomy>();

                var steamApps = await db.SteamApps.ToListAsync();
                if (!steamApps.Any())
                {
                    return;
                }

                var language = await db.SteamLanguages.FirstOrDefaultAsync(x => x.Id.ToString() == Constants.DefaultLanguageId);
                if (language == null)
                {
                    return;
                }

                var currency = await db.SteamCurrencies.FirstOrDefaultAsync(x => x.Id.ToString() == Constants.DefaultCurrencyId);
                if (currency == null)
                {
                    return;
                }

                var now = DateTime.UtcNow;
                var timeChecked = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
                foreach (var app in steamApps)
                {
                    var response = await steamEconomy.GetAssetPricesAsync(
                        UInt32.Parse(app.SteamId), currency.Name, language.SteamId
                    );
                    if (response?.Data?.Success != true)
                    {
                        // TODO: Log this...
                        continue;
                    }

                    foreach (var asset in response.Data.Assets)
                    {
                        await steamService.AddOrUpdateAppStoreItem(
                            app, currency, language, asset, timeChecked
                        );
                    }

                    await db.SaveChangesAsync();
                }
            }
        }

    }
}
