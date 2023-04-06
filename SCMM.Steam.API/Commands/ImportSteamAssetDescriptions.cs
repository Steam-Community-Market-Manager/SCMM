using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.WebApi.Models;
using SCMM.Steam.Data.Models.WebApi.Requests.ISteamEconomy;
using SCMM.Steam.Data.Models.WebApi.Requests.ISteamRemoteStorage;
using SCMM.Steam.Data.Store;
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
        private readonly SteamConfiguration _steamConfiguration;
        private readonly SteamDbContext _steamDb;
        private readonly SteamWebApiClient _steamWebApiClient;
        private readonly ICommandProcessor _commandProcessor;

        public ImportSteamAssetDescriptions(ILogger<ImportSteamAssetDescription> logger, IConfiguration cfg, SteamDbContext steamDb, SteamWebApiClient steamWebApiClient, ICommandProcessor commandProcessor)
        {
            _logger = logger;
            _steamConfiguration = cfg?.GetSteamConfiguration();
            _steamDb = steamDb;
            _steamWebApiClient = steamWebApiClient;
            _commandProcessor = commandProcessor;
        }

        public async Task<ImportSteamAssetDescriptionsResponse> HandleAsync(ImportSteamAssetDescriptionsRequest request)
        {
            // Get asset class info
            var assetClassInfoResponse = await _steamWebApiClient.SteamEconomyGetAssetClassInfo(new GetAssetClassInfoJsonRequest()
            {
                AppId = request.AppId,
                ClassIds = request.AssetClassIds
            });
            if (assetClassInfoResponse?.Any() != true)
            {
                throw new Exception($"Failed to get class info for assets, request failed");
            }
            var assetClasses = assetClassInfoResponse.Select(x => x.Value).ToArray();
            if (assetClasses == null)
            {
                throw new Exception($"Failed to get class info for assets, assets were not found");
            }

            // Get asset descriptions
            var assetDescriptions = await _steamDb.SteamAssetDescriptions
                .Include(x => x.App)
                .Where(x => request.AssetClassIds.Contains(x.ClassId ?? 0))
                .ToListAsync();

            // Add missing asset descriptions
            var missingAssetClasses = assetClasses.Where(x => !assetDescriptions.Any(y => y.ClassId == x.ClassId)).ToArray();
            foreach (var missingAssetClass in missingAssetClasses)
            {
                // Does a similiarly named item already exist?
                var assetDescription = await _steamDb.SteamAssetDescriptions.FirstOrDefaultAsync(x =>
                    x.App.SteamId == request.AppId.ToString() &&
                    x.ClassId == null &&
                    x.Name == missingAssetClass.Name
                );
                if (assetDescription == null)
                {
                    // Doesn't exist in database, create it now...
                    _steamDb.SteamAssetDescriptions.Add(assetDescription = new SteamAssetDescription()
                    {
                        App = await _steamDb.SteamApps.FirstOrDefaultAsync(x => x.SteamId == request.AppId.ToString()),
                        ClassId = missingAssetClass.ClassId,
                    });
                }
                assetDescriptions.Add(assetDescription);
            }

            Func<AssetClassInfo, ulong> parseWorkshopFileActionFromAssetClass = x =>
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

            var publishedFiles = (IEnumerable<PublishedFileDetails>)null;
            if (publishedFileIds.Any())
            {
                var publishedFileDetailsResponse = await _steamWebApiClient.SteamRemoteStorageGetPublishedFileDetails(new GetPublishedFileDetailsJsonRequest()
                {
                    PublishedFileIds = publishedFileIds.Select(x => x.Value).ToArray()
                });
                if (publishedFileDetailsResponse?.PublishedFileDetails == null)
                {
                    throw new Exception($"Failed to get workshop files for assets, response was empty");
                }
                publishedFiles = publishedFileDetailsResponse.PublishedFileDetails;
                if (!publishedFiles.Any())
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
