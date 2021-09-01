using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Azure.ServiceBus;
using SCMM.Steam.API.Messages;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.WebApi.Models;
using SCMM.Steam.Data.Models.WebApi.Requests.IPublishedFileService;
using SCMM.Steam.Data.Store;
using Steam.Models;
using Steam.Models.SteamEconomy;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SCMM.Steam.API.Commands
{
    public class ImportSteamAssetDescriptionRequest : ICommand<ImportSteamAssetDescriptionResponse>
    {
        public ulong AppId { get; set; }

        public ulong AssetClassId { get; set; }

        /// <summary>
        /// Optional, removes the need to lookup AssetClassId if supplied
        /// </summary>
        public SteamAssetClass AssetClass { get; set; }
    }

    public class ImportSteamAssetDescriptionResponse
    {
        /// <remarks>
        /// If asset does not exist, this will be null
        /// </remarks>
        public SteamAssetDescription AssetDescription { get; set; }
    }

    public class ImportSteamAssetDescription : ICommandHandler<ImportSteamAssetDescriptionRequest, ImportSteamAssetDescriptionResponse>
    {
        private readonly ILogger<ImportSteamAssetDescription> _logger;
        private readonly SteamDbContext _db;
        private readonly SteamConfiguration _cfg;
        private readonly SteamWebApiClient _apiClient;
        private readonly SteamCommunityWebClient _communityClient;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public ImportSteamAssetDescription(ILogger<ImportSteamAssetDescription> logger, SteamDbContext db, IConfiguration cfg, SteamWebApiClient apiClient, SteamCommunityWebClient communityClient, ServiceBusClient serviceBusClient, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _logger = logger;
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _apiClient = apiClient;
            _communityClient = communityClient;
            _serviceBusClient = serviceBusClient;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        public async Task<ImportSteamAssetDescriptionResponse> HandleAsync(ImportSteamAssetDescriptionRequest request)
        {
            var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_cfg.ApplicationKey);

            // Does this asset already exist?
            var assetDescription = await _db.SteamAssetDescriptions.FirstOrDefaultAsync(x => x.ClassId == request.AssetClassId);
            if (assetDescription == null)
            {
                // Doesn't exist in database, double check that it isn't transient (newly created)
                assetDescription = _db.SteamAssetDescriptions.Local.FirstOrDefault(x => x.ClassId == request.AssetClassId);
                if (assetDescription == null)
                {
                    // Definitally doesn't exist, create it now...
                    _db.SteamAssetDescriptions.Add(assetDescription = new SteamAssetDescription()
                    {
                        App = await _db.SteamApps.FirstOrDefaultAsync(x => x.SteamId == request.AppId.ToString()),
                        ClassId = request.AssetClassId,
                    });
                }
            }

            // Get asset class info
            var assetClass = (AssetClassInfoModel)null;
            if (request.AssetClass == null)
            {
                // We need to fetch it from Steam...
                var steamEconomy = steamWebInterfaceFactory.CreateSteamWebInterface<SteamEconomy>();
                var assetClassInfoResponse = await steamEconomy.GetAssetClassInfoAsync(
                    (uint)request.AppId,
                    new List<ulong>()
                    {
                        request.AssetClassId
                    }
                );
                if (assetClassInfoResponse?.Data?.Success != true)
                {
                    throw new Exception($"Failed to get class info for asset {request.AssetClassId}, request failed");
                }
                assetClass = assetClassInfoResponse.Data.AssetClasses.FirstOrDefault(x => x.ClassId == request.AssetClassId);
            }
            else
            {
                // It has already been fetched, map it to the description model...
                assetClass = new AssetClassInfoModel()
                {
                    IconUrl = request.AssetClass.IconUrl,
                    IconUrlLarge = request.AssetClass.IconUrlLarge,
                    Name = request.AssetClass.Name,
                    MarketHashName = request.AssetClass.MarketHashName,
                    MarketName = request.AssetClass.MarketName,
                    NameColor = request.AssetClass.NameColor,
                    BackgroundColor = request.AssetClass.BackgroundColor,
                    Type = request.AssetClass.Type,
                    Tradable = request.AssetClass.Tradable ? "1" : "0",
                    Marketable = request.AssetClass.Marketable ? "1" : "0",
                    Commodity = request.AssetClass.Commodity ? "1" : "0",
                    MarketTradableRestriction = request.AssetClass.MarketTradableRestriction,
                    MarketMarketableRestriction = request.AssetClass.MarketMarketableRestriction,
                    Descriptions = new ReadOnlyCollection<AssetClassDescriptionModel>(
                        new List<AssetClassDescriptionModel>(
                            request.AssetClass.Descriptions.Select(x => new AssetClassDescriptionModel()
                            {
                                Type = x.Type,
                                Value = x.Value
                            })
                        )
                    ),
                    Actions = new ReadOnlyCollection<AssetClassActionModel>(
                        new List<AssetClassActionModel>(
                            request.AssetClass.Actions.Select(x => new AssetClassActionModel()
                            {
                                Link = x.Link,
                                Name = x.Name
                            })
                        )
                    ),
                    Tags = new ReadOnlyCollection<AssetClassTagModel>(
                        new List<AssetClassTagModel>(
                            request.AssetClass.Tags.Select(x => new AssetClassTagModel()
                            {
                                Category = x.Category,
                                InternalName = x.InternalName,
                                CategoryName = x.LocalizedCategoryName,
                                Name = x.LocalizedTagName
                            })
                        )
                    ),
                    ClassId = request.AssetClass.ClassId,
                };
            }
            if (assetClass == null)
            {
                throw new Exception($"Failed to get class info for asset {request.AssetClassId}, asset was not found");
            }

            // Get item description text from asset class (if available)
            var itemDescription = assetClass.Descriptions?
                .Where(x =>
                    string.Equals(x.Type, Constants.SteamAssetClassDescriptionTypeHtml, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(x.Type, Constants.SteamAssetClassDescriptionTypeBBCode, StringComparison.InvariantCultureIgnoreCase)
                )
                .Select(x => x.Value)
                .FirstOrDefault();

            // Get published file details from Steam (if workshopfileid is available)
            var publishedFile = (PublishedFileDetailsModel)null;
            var publishedFileVotes = (PublishedFileVoteData)null;
            var publishedFilePreviews = (IEnumerable<PublishedFilePreview>)null;
            var publishedFileChangeNotesPageHtml = (XElement)null;
            var publishedFileId = (ulong)0;
            var publishedFileHasChanged = false;
            var viewWorkshopAction = assetClass.Actions?.FirstOrDefault(x =>
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
                var steamRemoteStorage = steamWebInterfaceFactory.CreateSteamWebInterface<SteamRemoteStorage>();
                var publishedFileDetails = await steamRemoteStorage.GetPublishedFileDetailsAsync(publishedFileId);
                if (publishedFileDetails?.Data == null)
                {
                    throw new Exception($"Failed to get workshop file {publishedFileId} for asset {request.AssetClassId}, response was empty");
                }

                publishedFile = publishedFileDetails.Data;
                publishedFileHasChanged = (assetDescription.TimeUpdated == null || assetDescription.TimeUpdated < publishedFile.TimeUpdated);
                
                // Get file vote data (if missing or item is not yet accepted, votes don't change once accepted)
                if ((assetDescription.VotesDown == null || assetDescription.VotesUp == null || !assetDescription.IsAccepted) && !string.IsNullOrEmpty(publishedFile.Title))
                {
                    var queryVoteData = await _apiClient.PublishedFileServiceQueryFiles(new QueryFilesJsonRequest()
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
                    // NOTE: We have to do two seperate calls here as for some strange reason Steam only returns vote counts if requested in isolation
                    var queryPreviews = await _apiClient.PublishedFileServiceQueryFiles(new QueryFilesJsonRequest()
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

                // Get change history (if missing or changed since our last check)
                if ((publishedFileHasChanged || !assetDescription.Changes.Any()) && assetDescription.TimeAccepted != null)
                {
                    publishedFileChangeNotesPageHtml = await _communityClient.GetHtml(new SteamWorkshopFileChangeNotesPageRequest()
                    {
                        Id = publishedFile.PublishedFileId.ToString()
                    });
                }
                
                // Queue a download of the workshop file data for analyse (if missing or changed since our last check)
                if (publishedFileHasChanged || string.IsNullOrEmpty(assetDescription.WorkshopFileUrl))
                {
                    await _serviceBusClient.SendMessageAsync(new DownloadSteamWorkshopFileMessage()
                    {
                        PublishedFileId = publishedFileId,
                        Force = publishedFileHasChanged
                    });
                }
            }

            // Get community market details from Steam (if item description or nameid is missing and it is a marketable item)
            var marketListingPageHtml = (string)null;
            var assetIsMarketable = string.Equals(assetClass.Marketable, "1", StringComparison.InvariantCultureIgnoreCase);
            if ((string.IsNullOrEmpty(itemDescription) || assetDescription.NameId == null) && assetIsMarketable)
            {
                marketListingPageHtml = await _communityClient.GetText(new SteamMarketListingPageRequest()
                {
                    AppId = request.AppId.ToString(),
                    MarketHashName = assetClass.MarketHashName,
                });
            }

            // Get store details from Steam (if item description is missing and it is a recently accepted store item)
            var storeItemPageHtml = (XElement)null;
            var assetIsRecentlyAccepted = (assetDescription.TimeAccepted != null && assetDescription.TimeAccepted >= DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14)));
            if (string.IsNullOrEmpty(itemDescription) && assetIsRecentlyAccepted)
            {
                var storeItems = await _communityClient.GetStorePaginated(new SteamStorePaginatedJsonRequest()
                {
                    AppId = request.AppId.ToString(),
                    Filter = SteamStorePaginatedJsonRequest.FilterAll,
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

                        storeItemPageHtml = await _communityClient.GetHtml(new SteamStoreItemPageRequest()
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

            return new ImportSteamAssetDescriptionResponse
            {
                AssetDescription = assetDescription
            };
        }
    }
}
