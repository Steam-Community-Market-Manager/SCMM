using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Store;
using Steam.Models;
using Steam.Models.SteamEconomy;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        private readonly SteamCommunityWebClient _client;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public ImportSteamAssetDescription(ILogger<ImportSteamAssetDescription> logger, SteamDbContext db, IConfiguration cfg, SteamCommunityWebClient client, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _logger = logger;
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _client = client;
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
                    String.Equals(x.Type, Constants.SteamAssetClassDescriptionTypeHtml, StringComparison.InvariantCultureIgnoreCase) || 
                    String.Equals(x.Type, Constants.SteamAssetClassDescriptionTypeBBCode, StringComparison.InvariantCultureIgnoreCase)
                )
                .Select(x => x.Value)
                .FirstOrDefault();

            // Get published file details from Steam (if workshopfileid is available)
            var publishedFile = (PublishedFileDetailsModel)null;
            var publishedFileId = (ulong)0;
            var viewWorkshopAction = assetClass.Actions?.FirstOrDefault(x =>
                String.Equals(x.Name, Constants.SteamActionViewWorkshopItemId, StringComparison.InvariantCultureIgnoreCase) ||
                String.Equals(x.Name, Constants.SteamActionViewWorkshopItem, StringComparison.InvariantCultureIgnoreCase)
            );
            if (viewWorkshopAction != null)
            {
                var workshopFileIdGroups = Regex.Match(viewWorkshopAction.Link, Constants.SteamActionViewWorkshopItemRegex).Groups;
                publishedFileId = (workshopFileIdGroups.Count > 1) ? UInt64.Parse(workshopFileIdGroups[1].Value) : 0;
            }
            if (publishedFileId > 0)
            {
                var steamRemoteStorage = steamWebInterfaceFactory.CreateSteamWebInterface<SteamRemoteStorage>();
                var publishedFileDetails = await steamRemoteStorage.GetPublishedFileDetailsAsync(publishedFileId);
                if (publishedFileDetails?.Data == null)
                {
                    throw new Exception($"Failed to get workshop file {publishedFileId} for asset {request.AssetClassId}, response was empty");
                }

                publishedFile = publishedFileDetails.Data;
            }

            // Get community market details from Steam (if item description or nameid is missing and it is a marketable item)
            var marketListingPageHtml = (string)null;
            var assetIsMarketable = String.Equals(assetClass.Marketable, "1", StringComparison.InvariantCultureIgnoreCase);
            if ((String.IsNullOrEmpty(itemDescription) || assetDescription.NameId == null) && assetIsMarketable)
            {
                marketListingPageHtml = await _client.GetText(new SteamMarketListingPageRequest()
                {
                    AppId = request.AppId.ToString(),
                    MarketHashName = assetClass.MarketHashName,
                });
            }

            // Get store details from Steam (if item description is missing and it is a recently accepted store item)
            var storeItemPageHtml = (XElement)null;
            var assetIsRecentlyAccepted = (assetDescription.TimeAccepted != null && assetDescription.TimeAccepted >= DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14)));
            if (String.IsNullOrEmpty(itemDescription) && assetIsRecentlyAccepted)
            {
                var storeItems = await _client.GetStorePaginated(new SteamStorePaginatedJsonRequest()
                {
                    AppId = request.AppId.ToString(),
                    Filter = SteamStorePaginatedJsonRequest.FilterAll,
                    SearchText = assetClass.MarketHashName,
                    Count = 1
                });
                if (storeItems?.Success == true && !String.IsNullOrEmpty(storeItems?.ResultsHtml))
                {
                    if (storeItems.ResultsHtml.Contains(assetClass.MarketHashName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var itemIdMatchGroup = Regex.Match(storeItems.ResultsHtml, Constants.SteamStoreItemDefLinkRegex).Groups;
                        var itemId = (itemIdMatchGroup.Count > 1)
                            ? itemIdMatchGroup[1].Value.Trim()
                            : null;

                        storeItemPageHtml = await _client.GetHtml(new SteamStoreItemPageRequest()
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
