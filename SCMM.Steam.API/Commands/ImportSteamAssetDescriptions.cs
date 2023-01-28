using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;
using Steam.Models;
using Steam.Models.SteamEconomy;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Text.RegularExpressions;

namespace SCMM.Steam.API.Commands
{
    public class ImportSteamAssetDescriptionsRequest : ICommand<ImportSteamAssetDescriptionsResponse>
    {
        public ulong AppId { get; set; }

        public ulong[] AssetClassIds { get; set; }
    }

    public class ImportSteamAssetDescriptionsResponse
    {
        public IEnumerable<SteamAssetDescription> AssetDescriptions { get; set; }
    }

    public class ImportSteamAssetDescriptions : ICommandHandler<ImportSteamAssetDescriptionsRequest, ImportSteamAssetDescriptionsResponse>
    {
        private readonly ILogger<ImportSteamAssetDescription> _logger;
        private readonly SteamDbContext _db;
        private readonly SteamConfiguration _cfg;
        private readonly ICommandProcessor _commandProcessor;

        public ImportSteamAssetDescriptions(ILogger<ImportSteamAssetDescription> logger, SteamDbContext db, IConfiguration cfg, ICommandProcessor commandProcessor)
        {
            _logger = logger;
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _commandProcessor = commandProcessor;
        }

        public async Task<ImportSteamAssetDescriptionsResponse> HandleAsync(ImportSteamAssetDescriptionsRequest request)
        {
            var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_cfg.ApplicationKey);

            // Get asset class info
            var steamEconomy = steamWebInterfaceFactory.CreateSteamWebInterface<SteamEconomy>();
            var assetClassInfoResponse = await steamEconomy.GetAssetClassInfoAsync(
                (uint)request.AppId,
                request.AssetClassIds
            );
            if (assetClassInfoResponse?.Data?.Success != true)
            {
                throw new Exception($"Failed to get class info for assets, request failed");
            }
            var assetClasses = assetClassInfoResponse.Data.AssetClasses?.ToArray();
            if (assetClasses == null)
            {
                throw new Exception($"Failed to get class info for assets, assets were not found");
            }

            // Get asset descriptions
            var assetDescriptions = await _db.SteamAssetDescriptions
                .Include(x => x.App)
                .Where(x => request.AssetClassIds.Contains(x.ClassId ?? 0))
                .ToListAsync();

            // Add missing asset descriptions
            var missingAssetClasses = assetClasses.Where(x => !assetDescriptions.Any(y => y.ClassId == x.ClassId)).ToArray();
            foreach (var missingAssetClass in missingAssetClasses)
            {
                // Does a similiarly named item already exist?
                var assetDescription = await _db.SteamAssetDescriptions.FirstOrDefaultAsync(x =>
                    x.App.SteamId == request.AppId.ToString() &&
                    x.ClassId == null &&
                    x.Name == missingAssetClass.Name
                );
                if (assetDescription == null)
                {
                    // Doesn't exist in database, create it now...
                    _db.SteamAssetDescriptions.Add(assetDescription = new SteamAssetDescription()
                    {
                        App = await _db.SteamApps.FirstOrDefaultAsync(x => x.SteamId == request.AppId.ToString()),
                        ClassId = missingAssetClass.ClassId,
                    });
                }
                assetDescriptions.Add(assetDescription);
            }

            Func<AssetClassInfoModel, ulong> parseWorkshopFileActionFromAssetClass = x =>
            {
                var viewWorkshopAction = x.Actions?.FirstOrDefault(x =>
                    string.Equals(x.Name, Constants.SteamActionViewWorkshopItemId, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(x.Name, Constants.SteamActionViewWorkshopItem, StringComparison.InvariantCultureIgnoreCase)
                );
                if (viewWorkshopAction != null)
                {
                    var workshopFileIdGroups = Regex.Match(viewWorkshopAction.Link, Constants.SteamActionViewWorkshopItemRegex).Groups;
                    return (workshopFileIdGroups.Count > 1) ? ulong.Parse(workshopFileIdGroups[1].Value) : 0;
                }

                return 0;
            };

            // Get published file details
            var publishedFileIds = assetClasses
                .ToDictionary(k => k, parseWorkshopFileActionFromAssetClass)
                .Where(x => x.Value > 0)
                .ToArray();

            var publishedFiles = (IEnumerable<PublishedFileDetailsModel>)null;
            if (publishedFileIds.Any())
            {
                var steamRemoteStorage = steamWebInterfaceFactory.CreateSteamWebInterface<SteamRemoteStorage>();
                var publishedFileDetails = await steamRemoteStorage.GetPublishedFileDetailsAsync(
                    (uint)publishedFileIds.Length,
                    publishedFileIds.Select(x => x.Value).ToArray()
                );
                if (publishedFileDetails?.Data == null)
                {
                    throw new Exception($"Failed to get workshop files for assets, response was empty");
                }
                publishedFiles = publishedFileDetails.Data.ToArray();
                if (assetClasses == null)
                {
                    throw new Exception($"Failed to get workshop files for assets, files were not found");
                }
            }

            // Update each asset description
            foreach (var assetDescription in assetDescriptions)
            {
                var updateAssetDescription = await _commandProcessor.ProcessWithResultAsync(new UpdateSteamAssetDescriptionRequest()
                {
                    AssetDescription = assetDescription,
                    AssetClass = assetClasses.FirstOrDefault(x => x.ClassId == assetDescription.ClassId),
                    PublishedFile = publishedFiles?.FirstOrDefault(x =>
                        x.PublishedFileId == publishedFileIds.FirstOrDefault(y =>
                            y.Key == assetClasses.FirstOrDefault(z => 
                                z.ClassId == assetDescription.ClassId
                            )
                        ).Value
                    )
                });
            }

            return new ImportSteamAssetDescriptionsResponse
            {
                AssetDescriptions = assetDescriptions
            };
        }
    }
}
