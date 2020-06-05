using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Shared;
using SCMM.Steam.Shared.Models;
using SCMM.Steam.Shared.Requests;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain.Models.Steam;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs
{
    public class CheckForNewSteamItemsJob : CronJobService
    {
        private readonly ILogger<CheckForNewSteamItemsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public CheckForNewSteamItemsJob(IConfiguration configuration, ILogger<CheckForNewSteamItemsJob> logger, IServiceScopeFactory scopeFactory)
            : base(configuration.GetJobConfiguration<CheckForNewSteamItemsJob>())
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                var steamClient = new SteamClient();
                var steamApps = db.SteamApps.ToImmutableList();
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

                // TODO: Error handling
                // TODO: Retry logic
                var pageRequests = new List<SteamMarketSearchPaginatedRequest>();
                foreach (var app in steamApps)
                {
                    var appPageCountRequest = new SteamMarketSearchPaginatedRequest()
                    {
                        AppId = app.SteamId,
                        Start = 1,
                        Count = 1,
                        Language = language.SteamId,
                        CurrencyId = currency.SteamId
                    };

                    var appPageRequests = await steamClient.GetMarketSearchPaginated(appPageCountRequest).ToObservable()
                        .Where(x => x?.Success == true && x?.TotalCount > 0)
                        .Select(x =>
                        {
                            var total = x.TotalCount;
                            var pageSize = SteamMarketSearchPaginatedRequest.MaxPageSize;
                            var requests = new List<SteamMarketSearchPaginatedRequest>();
                            for (var i = 0; i <= total; i += pageSize)
                            {
                                requests.Add(
                                    new SteamMarketSearchPaginatedRequest()
                                    {
                                        AppId = app.SteamId,
                                        Start = i,
                                        Count = Math.Min(total - i, pageSize),
                                        Language = language.SteamId,
                                        CurrencyId = currency.SteamId
                                    }
                                );
                            }
                            return requests;
                        });

                    if (appPageRequests.Any())
                    {
                        pageRequests.AddRange(appPageRequests);
                    }
                }

                // Add a 10 second delay between requests to avoid "you've made too many requests recently" error
                var newItems = await Observable.Interval(TimeSpan.FromSeconds(10))
                    .Zip(pageRequests, (x, y) => y)
                    .Select(x => Observable.FromAsync(() => steamClient.GetMarketSearchPaginated(x)))
                    .Merge()
                    .Where(x => x?.Success == true && x?.Results?.Count > 0)
                    .SelectMany(x => GetOrCreateSteamItems(db, x.Results))
                    .Where(x => x?.IsTransient == true)
                    .ToList();

                if (newItems.Any())
                {
                    await db.SaveChangesAsync();
                }
            }
        }

        public IEnumerable<SteamItem> GetOrCreateSteamItems(SteamDbContext db, IEnumerable<SteamMarketSearchItem> items)
        {
            foreach (var item in items)
            {
                yield return GetOrCreateSteamItem(db, item);
            }
        }

        public SteamItem GetOrCreateSteamItem(SteamDbContext db, SteamMarketSearchItem item)
        {
            if (String.IsNullOrEmpty(item?.AssetDescription.AppId))
            {
                return null;
            }

            var dbApp = db.SteamApps.FirstOrDefault(x => x.SteamId == item.AssetDescription.AppId);
            if (dbApp == null)
            {
                return null;
            }

            var dbItem = db.SteamItems.FirstOrDefault(x => x.Description != null && x.Description.SteamId == item.AssetDescription.ClassId);
            if (dbItem != null)
            {
                return dbItem;
            }

            dbApp.Items.Add(dbItem = new SteamItem()
            {
                App = dbApp,
                Name = item.AssetDescription.MarketName,
                Description = new SteamItemDescription()
                {
                    SteamId = item.AssetDescription.ClassId,
                    Name = item.AssetDescription.MarketNameHash,
                    BackgroundColour = item.AssetDescription.BackgroundColour.SteamColourToHexString(),
                    ForegroundColour = item.AssetDescription.NameColour.SteamColourToHexString(),
                    IconUrl = new SteamEconomyImageRequest(item.AssetDescription.IconUrl).Uri.ToString(),
                    IconLargeUrl = new SteamEconomyImageRequest(item.AssetDescription.IconUrlLarge).Uri.ToString()
                }
            });

            return dbItem;
        }
    }
}
