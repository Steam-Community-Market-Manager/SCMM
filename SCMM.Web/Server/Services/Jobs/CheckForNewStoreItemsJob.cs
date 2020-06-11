using AngleSharp.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Shared;
using SCMM.Steam.Shared.Community.Requests.Blob;
using SCMM.Steam.Shared.WebAPI.ISteamEconomy.GetAssetPrices;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Domain;
using SCMM.Web.Server.Domain.Models.Steam;
using SCMM.Web.Server.Services.Jobs.CronJob;
using Steam.Models.SteamEconomy;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
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

                foreach (var app in steamApps)
                {
                    var response = await steamEconomy.GetAssetPricesAsync(
                        UInt32.Parse(app.SteamId), currency.Name, language.SteamId
                    );
                    if (response?.Data?.Success != true)
                    {
                        continue;
                    }

                    foreach (var asset in response.Data.Assets)
                    {
                        await FindOrAddStoreItem(db, steamEconomy, app, currency, language, asset);
                    }

                    await db.SaveChangesAsync();
                }
            }
        }

        public async Task<SteamStoreItem> FindOrAddStoreItem(SteamDbContext db, SteamEconomy steamEconomy, SteamApp app, SteamCurrency currency, SteamLanguage language, AssetModel asset)
        {
            var dbItem = await db.SteamStoreItems
                .Include(x => x.Description)
                .Where(x => x.AppId == app.Id)
                .FirstOrDefaultAsync(x => x.SteamId == asset.Name);

            if (dbItem != null)
            {
                return dbItem;
            }

            var assetDescription = await db.SteamAssetDescriptions
                .FirstOrDefaultAsync(x => x.SteamId == asset.ClassId.ToString());

            if (assetDescription == null)
            {
                assetDescription = await FindOrAddAssetDescription(db, steamEconomy, app, language, asset.ClassId);
                if (assetDescription == null)
                {
                    return null;
                }
            }

            app.StoreItems.Add(dbItem = new SteamStoreItem()
            {
                SteamId = asset.Name,
                App = app,
                Description = assetDescription,
                Currency = currency,
                StorePrice = Int32.Parse(asset.Prices.ToDictionary().FirstOrDefault(x => x.Key == currency.Name).Value ?? "0")
            });

            return dbItem;
        }

        public async Task<SteamAssetDescription> FindOrAddAssetDescription(SteamDbContext db, SteamEconomy steamEconomy, SteamApp app, SteamLanguage language, ulong classId)
        {
            var response = await steamEconomy.GetAssetClassInfoAsync(
                UInt32.Parse(app.SteamId), new List<ulong>() { classId }, language.SteamId
            );
            if (response?.Data?.Success != true)
            {
                return null;
            }

            var assetDescription = response.Data.AssetClasses.FirstOrDefault(x => x.ClassId == classId);
            if (assetDescription == null)
            {
                return null;
            }

            var tags = new List<string>();
            var workshopFile = (SteamAssetWorkshopFile) null;
            var workshopFileId = (string)null;
            var viewWorkshopAction = assetDescription?.Actions?.FirstOrDefault(x => x.Name == SteamConstants.SteamActionViewWorkshopItem);
            if (viewWorkshopAction != null)
            {
                var workshopFileIdGroups = Regex.Match(viewWorkshopAction.Link, SteamConstants.SteamActionViewWorkshopItemRegex).Groups;
                workshopFileId = (workshopFileIdGroups.Count > 1) ? workshopFileIdGroups[1].Value : "0";

                var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_steamConfiguration.ApplicationKey);
                var steamRemoteStorage = steamWebInterfaceFactory.CreateSteamWebInterface<SteamRemoteStorage>();
                var fileResponse = await steamRemoteStorage.GetPublishedFileDetailsAsync(UInt32.Parse(workshopFileId));
                if (fileResponse?.Data != null)
                {
                    tags = fileResponse.Data.Tags.ToList();
                    workshopFile = new SteamAssetWorkshopFile()
                    {
                        SteamId = workshopFileId,
                        CreatedOn = fileResponse.Data.TimeCreated,
                        UpdatedOn = fileResponse.Data.TimeUpdated,
                        Subscriptions = (int) fileResponse.Data.LifetimeSubscriptions,
                        Views = (int) fileResponse.Data.Views
                    };
                }
            }

            var dbAssetDescription = new SteamAssetDescription()
            {
                SteamId = assetDescription.ClassId.ToString(),
                Name = assetDescription.MarketName,
                BackgroundColour = assetDescription.BackgroundColor.SteamColourToHexString(),
                ForegroundColour = assetDescription.NameColor.SteamColourToHexString(),
                IconUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrl).Uri.ToString(),
                IconLargeUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrlLarge).Uri.ToString(),
                WorkshopFile = workshopFile,
                Tags = new Data.Types.PersistableStringCollection(tags)
            };

            db.SteamAssetDescriptions.Add(dbAssetDescription);
            return dbAssetDescription;
        }
    }
}
