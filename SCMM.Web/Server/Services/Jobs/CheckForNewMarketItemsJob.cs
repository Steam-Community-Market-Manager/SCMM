using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Shared.Community.Requests.Json;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class CheckForNewMarketItemsJob : CronJobService
    {
        private readonly ILogger<CheckForNewMarketItemsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public CheckForNewMarketItemsJob(IConfiguration configuration, ILogger<CheckForNewMarketItemsJob> logger, IServiceScopeFactory scopeFactory)
            : base(configuration.GetJobConfiguration<CheckForNewMarketItemsJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var steamClient = new SteamCommunityClient();
                var steamService = scope.ServiceProvider.GetRequiredService<SteamService>();
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                
                var steamApps = db.SteamApps.ToList();
                if (!steamApps.Any())
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

                var pageRequests = new List<SteamMarketSearchPaginatedJsonRequest>();
                foreach (var app in steamApps)
                {
                    var appPageCountRequest = new SteamMarketSearchPaginatedJsonRequest()
                    {
                        AppId = app.SteamId,
                        Start = 1,
                        Count = 1,
                        Language = language.SteamId,
                        CurrencyId = currency.SteamId
                    };

                    var appPageCountResponse = await steamClient.GetMarketSearchPaginated(appPageCountRequest);
                    if (appPageCountResponse?.Success != true || appPageCountResponse?.TotalCount <= 0)
                    {
                        continue;
                    }

                    var total = appPageCountResponse.TotalCount;
                    var pageSize = SteamMarketSearchPaginatedJsonRequest.MaxPageSize;
                    var appPageRequests = new List<SteamMarketSearchPaginatedJsonRequest>();
                    for (var i = 0; i <= total; i += pageSize)
                    {
                        appPageRequests.Add(
                            new SteamMarketSearchPaginatedJsonRequest()
                            {
                                AppId = app.SteamId,
                                Start = i,
                                Count = Math.Min(total - i, pageSize),
                                Language = language.SteamId,
                                CurrencyId = currency.SteamId
                            }
                        );
                    }

                    if (appPageRequests.Any())
                    {
                        pageRequests.AddRange(appPageRequests);
                    }
                }

                // Add a 10 second delay between requests to avoid "Too Many Requests" error
                var newItems = await Observable.Interval(TimeSpan.FromSeconds(10))
                    .Zip(pageRequests, (x, y) => y)
                    .Select(x => Observable.FromAsync(() => steamClient.GetMarketSearchPaginated(x)))
                    .Merge()
                    .Where(x => x?.Success == true && x?.Results?.Count > 0)
                    .SelectMany(x => steamService.FindOrAddSteamItems(x.Results))
                    .Where(x => x?.IsTransient == true)
                    .ToList();

                if (newItems.Any())
                {
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
