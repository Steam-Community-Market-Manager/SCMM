using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Shared;
using SCMM.Steam.Shared.Community.Requests.Html;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Services.Jobs.CronJob;
using Skclusive.Material.List;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SCMM.Web.Server.Services.Jobs
{
    public class UpdateStoreTopSellerStatisticsJob : CronJobService
    {
        private readonly ILogger<UpdateStoreTopSellerStatisticsJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public UpdateStoreTopSellerStatisticsJob(IConfiguration configuration, ILogger<UpdateStoreTopSellerStatisticsJob> logger, IServiceScopeFactory scopeFactory)
            : base(logger, configuration.GetJobConfiguration<UpdateStoreTopSellerStatisticsJob>())
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
                var appStoreItems = db.SteamStoreItems
                    .Include(x => x.App)
                    .Include(x => x.Description.WorkshopFile)
                    .Where(x => x.Description.WorkshopFile.AcceptedOn == x.App.StoreItems.Max(x => x.Description.WorkshopFile.AcceptedOn))
                    .ToList();

                var appStores = appStoreItems.GroupBy(x => x.App.SteamId).ToList();
                foreach (var appStore in appStores)
                {
                    _logger.LogInformation($"Updating store item top seller statistics (app: {appStore.Key})");
                    var storePage = await commnityClient.GetItemStorePage(new SteamItemStorePageRequest()
                    {
                        AppId = appStore.Key,
                        Start = 0,
                        Count = SteamItemStorePageRequest.MaxPageSize,
                        Filter = SteamItemStorePageRequest.FilterFeatured
                    });
                    if (storePage == null)
                    {
                        _logger.LogError("Failed to get item store details");
                        continue;
                    }

                    var storeItemIds = new List<string>();
                    var storeItemDefs = storePage.Descendants()
                        .Where(x => x.Attribute("class")?.Value?.Contains(SteamConstants.SteamStoreItemDef) == true);
                    foreach (var storeItemDef in storeItemDefs)
                    {
                        var storeItemName = storeItemDef.Descendants()
                            .FirstOrDefault(x => x.Attribute("class")?.Value?.Contains(SteamConstants.SteamStoreItemDefName) == true);
                        if (storeItemName != null)
                        {
                            var storeItemLink = storeItemName.Descendants()
                                .Where(x => x.Name.LocalName == "a")
                                .Select(x => x.Attribute("href"))
                                .FirstOrDefault();
                            if (!String.IsNullOrEmpty(storeItemLink?.Value))
                            {
                                var storeItemIdMatchGroup = Regex.Match(storeItemLink.Value, SteamConstants.SteamStoreItemDefLinkRegex).Groups;
                                var storeItemId = (storeItemIdMatchGroup.Count > 1)
                                    ? storeItemIdMatchGroup[1].Value.Trim()
                                    : null;
                                if (!String.IsNullOrEmpty(storeItemId))
                                {
                                    storeItemIds.Add(storeItemId);
                                }
                            }
                        }
                    }

                    var storeItems = appStore.ToArray();
                    foreach (var storeItemId in storeItemIds)
                    {
                        var storeItem = storeItems.FirstOrDefault(x => x.SteamId == storeItemId);
                        if (storeItem == null)
                        {
                            continue;
                        }

                        var storeRankPosition = (storeItemIds.IndexOf(storeItemId) + 1);
                        var storeRankTotal = storeItemIds.Count;
                        storeItem = steamService.UpdateStoreItemRank(storeItem, storeRankPosition, storeRankTotal);
                    }

                    // Calculate total sales twice, once in both directions (to get correct min/max values)
                    foreach (var storeItem in storeItems.OrderByDescending(x => x.StoreRankPosition))
                    {
                        storeItem.RecalculateTotalSales(storeItems);
                    }
                    foreach (var storeItem in storeItems.OrderBy(x => x.StoreRankPosition))
                    {
                        storeItem.RecalculateTotalSales(storeItems);
                    }

                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
