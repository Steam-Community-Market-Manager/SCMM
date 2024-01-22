using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.API.Messages;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Models.Store.Requests.Html;
using SCMM.Steam.Data.Models.Store.Requests.Json;
using SCMM.Steam.Data.Models.WebApi.Models;
using SCMM.Steam.Data.Models.WebApi.Requests.IPublishedFileService;
using SCMM.Steam.Data.Models.WebApi.Requests.ISteamEconomy;
using SCMM.Steam.Data.Models.WebApi.Requests.ISteamRemoteStorage;
using SCMM.Steam.Data.Store;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SCMM.Steam.API.Commands
{
    public class ImportSteamAssetDescriptionRequest : ICommand<ImportSteamAssetDescriptionResponse>
    {
        public ulong AppId { get; set; }

        public ulong AssetClassId { get; set; }
    }

    public class ImportSteamAssetDescriptionResponse
    {
        public SteamAssetDescription AssetDescription { get; set; }
    }

    public class ImportSteamAssetDescription : ICommandHandler<ImportSteamAssetDescriptionRequest, ImportSteamAssetDescriptionResponse>
    {
        private readonly ILogger<ImportSteamAssetDescription> _logger;
        private readonly SteamDbContext _db;
        private readonly SteamConfiguration _cfg;
        private readonly SteamWebApiClient _apiClient;
        private readonly SteamCommunityWebClient _communityClient;
        private readonly SteamStoreWebClient _storeClient;
        private readonly IServiceBus _serviceBus;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public ImportSteamAssetDescription(ILogger<ImportSteamAssetDescription> logger, SteamDbContext db, IConfiguration cfg, SteamWebApiClient apiClient, SteamCommunityWebClient communityClient, SteamStoreWebClient storeClient, IServiceBus serviceBus, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _logger = logger;
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _apiClient = apiClient;
            _communityClient = communityClient;
            _storeClient = storeClient;
            _serviceBus = serviceBus;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        public async Task<ImportSteamAssetDescriptionResponse> HandleAsync(ImportSteamAssetDescriptionRequest request)
        {
            // Get asset class info
            var assetClassInfoResponse = await _apiClient.SteamEconomyGetAssetClassInfoAsync(new GetAssetClassInfoJsonRequest()
            {
                AppId = request.AppId,
                ClassIds = new[] { request.AssetClassId }
            });
            if (assetClassInfoResponse?.Success != true)
            {
                throw new Exception($"Failed to get class info for asset {request.AssetClassId}, request failed");
            }

            var assetClass = assetClassInfoResponse.Assets.FirstOrDefault(x => x.ClassId == request.AssetClassId.ToString());
            if (assetClass == null)
            {
                throw new Exception($"Failed to get class info for asset {request.AssetClassId}, asset was not found");
            }

            // Does this asset already exist?
            var assetDescription = (await _db.SteamAssetDescriptions.Include(x => x.App).FirstOrDefaultAsync(x => x.ClassId.ToString() == assetClass.ClassId)) ??
                                   (_db.SteamAssetDescriptions.Local.FirstOrDefault(x => x.ClassId?.ToString() == assetClass.ClassId));
            if (assetDescription == null)
            {
                // Does a similiarly named item already exist?
                assetDescription = await _db.SteamAssetDescriptions.FirstOrDefaultAsync(x =>
                    x.App.SteamId == request.AppId.ToString() &&
                    x.ClassId == null &&
                    x.Name == assetClass.Name
                );
                if (assetDescription == null)
                {
                    // Doesn't exist in database, create it now...
                    _db.SteamAssetDescriptions.Add(assetDescription = new SteamAssetDescription()
                    {
                        App = await _db.SteamApps.FirstOrDefaultAsync(x => x.SteamId == request.AppId.ToString()),
                        ClassId = UInt64.Parse(assetClass.ClassId),
                    });
                }
            }

            // Get published file details from Steam (if workshopfileid is available)
            var publishedFile = (PublishedFileDetails)null;
            var publishedFileVotes = (PublishedFileVoteData)null;
            var publishedFilePreviews = (IEnumerable<PublishedFilePreview>)null;
            var publishedFileChangeNotesPageHtml = (XElement)null;
            var publishedFileId = (ulong)0;
            var publishedFileHasChanged = false;
            var viewWorkshopAction = assetClass.Actions?.Select(x => x.Value)?.FirstOrDefault(x =>
                string.Equals(x.Name, Constants.SteamActionViewWorkshopItemId, StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(x.Name, Constants.SteamActionViewWorkshopItem, StringComparison.InvariantCultureIgnoreCase)
            );
            if (viewWorkshopAction != null)
            {
                var workshopFileIdGroups = Regex.Match(viewWorkshopAction.Link, Constants.SteamActionViewWorkshopItemRegex).Groups;
                publishedFileId = (workshopFileIdGroups.Count > 1) ? ulong.Parse(workshopFileIdGroups[1].Value) : 0;
            }
            if (publishedFileId > 0)
            {
                // Get file details
                var publishedFileDetailsResponse = await _apiClient.SteamRemoteStorageGetPublishedFileDetailsAsync(new GetPublishedFileDetailsJsonRequest()
                {
                    PublishedFileIds = new[] { publishedFileId }
                });
                var publishedFileDetails = publishedFileDetailsResponse?.PublishedFileDetails?.FirstOrDefault(x => x.PublishedFileId == publishedFileId);
                if (publishedFileDetails == null)
                {
                    throw new Exception($"Failed to get workshop file {publishedFileId} for asset {request.AssetClassId}, response was empty");
                }

                publishedFile = publishedFileDetails;
                publishedFileHasChanged = (assetDescription.TimeUpdated == null || assetDescription.TimeUpdated < publishedFile.TimeUpdated.SteamTimestampToDateTimeOffset());

                // Get file vote data (if missing or item is not yet accepted, votes don't change once accepted)
                if ((assetDescription.VotesDown == null || assetDescription.VotesUp == null || !assetDescription.IsAccepted) && !string.IsNullOrEmpty(publishedFile.Title))
                {
                    // NOTE: We have to do two seperate calls to "QueryFiles" as for some strange reason Steam only returns vote counts if requested in isolation
                    var queryVoteData = await _apiClient.PublishedFileServiceQueryFilesAsync(new QueryFilesJsonRequest()
                    {
                        QueryType = QueryFilesJsonRequest.QueryTypeRankedByTextSearch,
                        SearchText = publishedFile.Title,
                        AppId = publishedFile.ConsumerAppId,
                        Page = 0,
                        NumPerPage = 3,
                        ReturnVoteData = true
                    });

                    publishedFileVotes = queryVoteData?.PublishedFileDetails?.FirstOrDefault(x => x.PublishedFileId == publishedFile.PublishedFileId)?.VoteData;
                }

                // Get file previews (if missing or changed since our last check)
                if ((publishedFileHasChanged || !assetDescription.Previews.Any()) && !string.IsNullOrEmpty(publishedFile.Title))
                {
                    // NOTE: We have to do two seperate calls to "QueryFiles" as for some strange reason Steam only returns vote counts if requested in isolation
                    var queryPreviews = await _apiClient.PublishedFileServiceQueryFilesAsync(new QueryFilesJsonRequest()
                    {
                        QueryType = QueryFilesJsonRequest.QueryTypeRankedByTextSearch,
                        SearchText = publishedFile.Title,
                        AppId = publishedFile.ConsumerAppId,
                        Page = 0,
                        NumPerPage = 3,
                        ReturnPreviews = true
                    });

                    publishedFilePreviews = queryPreviews?.PublishedFileDetails?.FirstOrDefault(x => x.PublishedFileId == publishedFile.PublishedFileId)?.Previews;
                }

                // Get change history (if changed since our last check)
                if (publishedFileHasChanged && assetDescription.TimeAccepted != null)
                {
                    publishedFileChangeNotesPageHtml = await _communityClient.GetHtmlAsync(new SteamWorkshopFileChangeNotesPageRequest()
                    {
                        Id = publishedFile.PublishedFileId.ToString()
                    });
                }
            }

            var assetClassHasItemDescription = assetClass.Descriptions?.Select(x => x.Value)?
                .Where(x =>
                    string.Equals(x.Type, Constants.SteamAssetClassDescriptionTypeHtml, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(x.Type, Constants.SteamAssetClassDescriptionTypeBBCode, StringComparison.InvariantCultureIgnoreCase)
                )
                .Select(x => x.Value)
                .Where(x => !String.IsNullOrEmpty(x))
                .Any() ?? false;

            // Get community market details from Steam (if item description or nameid is missing and it is a marketable item)
            var marketListingPageHtml = (string)null;
            var assetIsMarketable = (
                string.Equals(assetClass.Marketable, "1", StringComparison.InvariantCultureIgnoreCase) ||
                (string.IsNullOrEmpty(assetClass.Marketable) && !string.IsNullOrEmpty(assetClass.MarketHashName))
            );
            var needsDescription = (!assetClassHasItemDescription && string.IsNullOrEmpty(assetDescription.Description));
            var needsNameId = (assetDescription.NameId == null);
            if (assetIsMarketable && (needsDescription || needsNameId))
            {
                marketListingPageHtml = await _communityClient.GetTextAsync(new SteamMarketListingPageRequest()
                {
                    AppId = request.AppId.ToString(),
                    MarketHashName = assetClass.MarketHashName,
                });
            }

            // Get store details from Steam (if item description is missing and it is a recently accepted store item)
            var storeItemPageHtml = (XElement)null;
            var assetIsRecentlyAccepted = (assetDescription.TimeAccepted != null && assetDescription.TimeAccepted >= DateTimeOffset.Now.Subtract(TimeSpan.FromDays(10)));
            if (assetIsRecentlyAccepted && needsDescription)
            {
                var storeItems = await _storeClient.GetStorePaginatedAsync(new SteamItemStoreGetItemDefsPaginatedJsonRequest()
                {
                    AppId = request.AppId.ToString(),
                    Filter = SteamItemStoreGetItemDefsPaginatedJsonRequest.FilterAll,
                    SearchText = assetClass.MarketHashName,
                    Count = 1
                });
                if (storeItems?.Success == true && !string.IsNullOrEmpty(storeItems?.ResultsHtml))
                {
                    if (storeItems.ResultsHtml.Contains(assetClass.MarketHashName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var itemIdMatchGroup = Regex.Match(storeItems.ResultsHtml, Constants.SteamStoreItemDefLinkRegex).Groups;
                        var itemId = (itemIdMatchGroup.Count > 1)
                            ? itemIdMatchGroup[1].Value.Trim()
                            : null;

                        storeItemPageHtml = await _storeClient.GetStoreDetailPageAsync(new SteamItemStoreDetailPageRequest()
                        {
                            AppId = request.AppId.ToString(),
                            ItemId = itemId,
                        });
                    }
                }
            }

            // Update the asset description
            var updateAssetDescription = await _commandProcessor.ProcessWithResultAsync(new UpdateSteamAssetDescriptionRequest()
            {
                AssetDescription = assetDescription,
                AssetClass = assetClass,
                PublishedFile = publishedFile,
                PublishedFileVoteData = publishedFileVotes,
                PublishedFilePreviews = publishedFilePreviews,
                PublishedFileChangeNotesPageHtml = publishedFileChangeNotesPageHtml,
                MarketListingPageHtml = marketListingPageHtml,
                StoreItemPageHtml = storeItemPageHtml
            });

            // If the asset description is now persistent (not transient)...
            if (!assetDescription.IsTransient)
            {
                // Queue a download of the workshop file data for analyse (if it's missing or has changed since our last check)
                if (publishedFileId > 0 && (publishedFileHasChanged || string.IsNullOrEmpty(assetDescription.WorkshopFileUrl)) && !assetDescription.WorkshopFileIsUnavailable)
                {
                    var app = await _db.SteamApps.FirstOrDefaultAsync(x => x.SteamId == request.AppId.ToString());
                    if (app?.FeatureFlags.HasFlag(SteamAppFeatureFlags.ItemWorkshop) == true)
                    {
                        await _serviceBus.SendMessageAsync(new ImportWorkshopFileContentsMessage()
                        {
                            AppId = request.AppId,
                            PublishedFileId = publishedFileId,
                            Force = publishedFileHasChanged
                        });
                    }
                }
            }

            return new ImportSteamAssetDescriptionResponse
            {
                AssetDescription = assetDescription
            };
        }
    }
}
