using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Shared;
using SCMM.Steam.Shared.Community.Models;
using SCMM.Steam.Shared.Community.Requests.Blob;
using SCMM.Steam.Shared.Community.Requests.Json;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain.Models.Steam;
using SCMM.Web.Server.Services.Jobs.CronJob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
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
                var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
                var steamClient = new SteamCommunityClient();

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
                    .SelectMany(x => FindOrAddSteamItems(db, x.Results))
                    .Where(x => x?.IsTransient == true)
                    .ToList();

                if (newItems.Any())
                {
                    await db.SaveChangesAsync();
                }
            }
        }

        public IEnumerable<SteamMarketItem> FindOrAddSteamItems(SteamDbContext db, IEnumerable<SteamMarketSearchItem> items)
        {
            foreach (var item in items)
            {
                yield return FindOrAddSteamItem(db, item);
            }
        }

        public SteamMarketItem FindOrAddSteamItem(SteamDbContext db, SteamMarketSearchItem item)
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

            var dbItem = db.SteamMarketItems
                .Include(x => x.Description)
                .FirstOrDefault(x => x.Description != null && x.Description.SteamId == item.AssetDescription.ClassId);

            if (dbItem != null)
            {
                return dbItem;
            }

            var workshopFileId = (string) null;
            var viewWorkshopAction = item.AssetDescription?.Actions?.FirstOrDefault(x => x.Name == SteamConstants.SteamActionViewWorkshopItem);
            if (viewWorkshopAction != null)
            {
                var workshopFileIdGroups = Regex.Match(viewWorkshopAction.Link, SteamConstants.SteamActionViewWorkshopItemRegex).Groups;
                workshopFileId = (workshopFileIdGroups.Count > 1) ? workshopFileIdGroups[1].Value : "0";
            }

            dbApp.MarketItems.Add(dbItem = new SteamMarketItem()
            {
                App = dbApp,
                Description = new Domain.Models.Steam.SteamAssetDescription()
                {
                    SteamId = item.AssetDescription.ClassId,
                    Name = item.AssetDescription.MarketName,
                    BackgroundColour = item.AssetDescription.BackgroundColour.SteamColourToHexString(),
                    ForegroundColour = item.AssetDescription.NameColour.SteamColourToHexString(),
                    IconUrl = new SteamEconomyImageBlobRequest(item.AssetDescription.IconUrl).Uri.ToString(),
                    IconLargeUrl = new SteamEconomyImageBlobRequest(item.AssetDescription.IconUrlLarge).Uri.ToString(),
                    WorkshopFile = String.IsNullOrEmpty(workshopFileId) ? null : new SteamAssetWorkshopFile()
                    {
                        SteamId = workshopFileId
                    }
                }
            });

            return dbItem;
        }
    }
}
