using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using Steam.Models;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using SCMM.Steam.Data.Models.Community.Requests.Blob;
using Newtonsoft.Json;

namespace SCMM.Steam.API.Commands
{
    public class ImportSteamAssetDescriptionRequest : ICommand<ImportSteamAssetDescriptionResponse>
    {
        public ulong AppId { get; set; }

        public ulong AssetId { get; set; }
    }

    public class ImportSteamAssetDescriptionResponse
    {
        /// <remarks>
        /// If asset does not exist, this will be null
        /// </remarks>
        public SteamAssetDescription Asset { get; set; }
    }

    public class ImportSteamAssetDescription : ICommandHandler<ImportSteamAssetDescriptionRequest, ImportSteamAssetDescriptionResponse>
    {
        private readonly string[] CommonItemWords = { "Pants", "Vest", "AR" };

        private readonly ILogger<ImportSteamAssetDescription> _logger;
        private readonly SteamDbContext _db;
        private readonly SteamConfiguration _cfg;
        private readonly SteamCommunityWebClient _client;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public ImportSteamAssetDescription(ILogger<ImportSteamAssetDescription> logger, SteamDbContext db, IConfiguration cfg, SteamCommunityWebClient client, IQueryProcessor queryProcessor, ICommandProcessor commandProcessor)
        {
            _logger = logger;
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _client = client;
            _queryProcessor = queryProcessor;
            _commandProcessor = commandProcessor;
        }

        public async Task<ImportSteamAssetDescriptionResponse> HandleAsync(ImportSteamAssetDescriptionRequest request)
        {
            var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_cfg.ApplicationKey);

            // Does this asset already exist?
            var asset = await _db.SteamAssetDescriptions.FirstOrDefaultAsync(x => x.AssetId == request.AssetId);
            if (asset == null)
            {
                // Doesn't exist in database, double check that it isn't transient (newly created)
                asset = _db.SteamAssetDescriptions.Local.FirstOrDefault(x => x.AssetId == request.AssetId);
                if (asset == null)
                {
                    // Definitally doesn't exist, create it now...
                    _db.SteamAssetDescriptions.Add(asset = new SteamAssetDescription()
                    {
                        AssetId = request.AssetId
                    });
                }
            }

            //
            // Get asset class info
            //
            var steamEconomy = steamWebInterfaceFactory.CreateSteamWebInterface<SteamEconomy>();
            var assetClassInfo = await steamEconomy.GetAssetClassInfoAsync(
                (uint)request.AppId, new List<ulong>() { request.AssetId }
            );
            if (assetClassInfo?.Data?.Success != true)
            {
                throw new Exception($"Failed to get class info for asset {request.AssetId}, request failed");
            }

            var assetDescription = assetClassInfo?.Data?.AssetClasses?.FirstOrDefault(x => x.ClassId == request.AssetId);
            if (assetDescription == null)
            {
                throw new Exception($"Failed to get class info for asset {request.AssetId}, asset was not found");
            }

            var viewWorkshopAction = assetDescription.Actions?.FirstOrDefault(x => x.Name == Constants.SteamActionViewWorkshopItem);
            if (viewWorkshopAction != null)
            {
                var workshopFileIdGroups = Regex.Match(viewWorkshopAction.Link, Constants.SteamActionViewWorkshopItemRegex).Groups;
                asset.WorkshopFileId = (workshopFileIdGroups.Count > 1) ? UInt64.Parse(workshopFileIdGroups[1].Value) : 0;
                asset.AssetType = SteamAssetDescriptionType.WorkshopItem;
            }

            //
            // Get published file details (if workshopfileid is available)
            //
            var publishedFile = (PublishedFileDetailsModel)null;
            if (asset.WorkshopFileId != null && asset.WorkshopFileId > 0)
            {
                var steamRemoteStorage = steamWebInterfaceFactory.CreateSteamWebInterface<SteamRemoteStorage>();
                var publishedFileDetails = await steamRemoteStorage.GetPublishedFileDetailsAsync(asset.WorkshopFileId.Value);
                if (publishedFileDetails?.Data == null)
                {
                    throw new Exception($"Failed to get workshop file {asset.WorkshopFileId} for asset {request.AssetId}, response was empty");
                }

                publishedFile = publishedFileDetails.Data;
            }

            //
            // Get community market details (if description or nameid is missing and it is a marketable item)
            //
            var marketListingPageHtml = (string)null;
            var assetIsMarketable = String.Equals(assetDescription.Marketable, "1", StringComparison.InvariantCultureIgnoreCase);
            if ((String.IsNullOrEmpty(asset.Description) || asset.NameId == null) && assetIsMarketable)
            {
                marketListingPageHtml = await _client.GetText(new SteamMarketListingPageRequest()
                {
                    AppId = request.AppId.ToString(),
                    MarketHashName = assetDescription.MarketHashName,
                });
            }

            //
            // Get store details (if description is missing and it is a recently accepted store item)
            //
            var storeItemPageHtml = (XElement)null;
            var assetIsRecentlyAccepted = (asset.TimeAccepted != null && asset.TimeAccepted >= DateTimeOffset.Now.Subtract(TimeSpan.FromDays(30)));
            if (String.IsNullOrEmpty(asset.Description) && assetIsRecentlyAccepted)
            {
                var storeItems = await _client.GetStorePaginated(new SteamStorePaginatedJsonRequest()
                {
                    AppId = request.AppId.ToString(),
                    Filter = SteamStorePaginatedJsonRequest.FilterAll,
                    SearchText = assetDescription.MarketHashName,
                    Count = 1
                });
                if (storeItems?.Success == true && !String.IsNullOrEmpty(storeItems?.ResultsHtml))
                {
                    if (storeItems.ResultsHtml.Contains(assetDescription.MarketHashName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var itemIdMatchGroup = Regex.Match(storeItems.ResultsHtml, @"\/detail\/(\d+)").Groups;
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

            // Parse common asset details
            asset.Name = assetDescription.MarketName;
            asset.NameHash = assetDescription.MarketHashName;
            asset.IconUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrl);
            asset.IconLargeUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrlLarge ?? assetDescription.IconUrl);
            asset.ImageUrl = publishedFile?.PreviewUrl?.ToString();
            asset.BackgroundColour = assetDescription.BackgroundColor?.SteamColourToHexString();
            asset.ForegroundColour = assetDescription.NameColor?.SteamColourToHexString();
            asset.CurrentSubscriptions = (long?)publishedFile?.Subscriptions;
            asset.TotalSubscriptions = (long?)publishedFile?.LifetimeSubscriptions;
            asset.IsCommodity = String.Equals(assetDescription.Commodity, "1", StringComparison.InvariantCultureIgnoreCase);
            asset.IsMarketable = String.Equals(assetDescription.Marketable, "1", StringComparison.InvariantCultureIgnoreCase);
            asset.MarketableRestrictionDays = String.IsNullOrEmpty(assetDescription.MarketMarketableRestriction) ? (int?)null : Int32.Parse(assetDescription.MarketMarketableRestriction);
            asset.IsTradable = String.Equals(assetDescription.Tradable, "1", StringComparison.InvariantCultureIgnoreCase);
            asset.TradableRestrictionDays = String.IsNullOrEmpty(assetDescription.MarketTradableRestriction) ? (int?)null : Int32.Parse(assetDescription.MarketTradableRestriction);
            asset.IsBanned = publishedFile?.Banned ?? false;
            asset.BanReason = publishedFile?.BanReason;
            asset.TimeCreated = publishedFile?.TimeCreated;
            asset.TimeUpdated = publishedFile?.TimeUpdated;

            // Parse asset type
            switch (assetDescription.Type)
            {
                case "Workshop Item": asset.AssetType = SteamAssetDescriptionType.WorkshopItem; break;
            }

            // Parse asset app
            if (asset.AppId == Guid.Empty)
            {
                asset.App = await _db.SteamApps.FirstOrDefaultAsync(x => x.SteamId == request.AppId.ToString());
            }

            // Parse asset creator
            if (asset.CreatorId == null && publishedFile?.Creator != null)
            {
                var importedProfile = await _commandProcessor.ProcessWithResultAsync(
                    new ImportSteamProfileRequest()
                    {
                        ProfileId = publishedFile.Creator.ToString()
                    }
                );

                asset.Creator = importedProfile?.Profile;
            }

            // Parse asset icon and image data
            if (asset.IconId == null && !String.IsNullOrEmpty(asset.IconUrl))
            {
                var importedImage = await _commandProcessor.ProcessWithResultAsync(new ImportImageDataRequest()
                {
                    Url = asset.IconUrl,
                    UseExisting = true
                });
                if (importedImage?.Image != null)
                {
                    asset.Icon = importedImage.Image;
                }
            }
            if (asset.IconLargeId == null && !String.IsNullOrEmpty(asset.IconLargeUrl))
            {
                var importedImage = await _commandProcessor.ProcessWithResultAsync(new ImportImageDataRequest()
                {
                    Url = asset.IconLargeUrl,
                    UseExisting = true
                });
                if (importedImage?.Image != null)
                {
                    asset.IconLarge = importedImage.Image;
                }
            }
            if (asset.ImageId == null && !String.IsNullOrEmpty(asset.ImageUrl))
            {
                var importedImage = await _commandProcessor.ProcessWithResultAsync(new ImportImageDataRequest()
                {
                    Url = asset.ImageUrl,
                    UseExisting = true
                });
                if (importedImage?.Image != null)
                {
                    asset.Image = importedImage.Image;
                }
            }

            // Parse asset description and name id from the market list page (if available)
            if (!String.IsNullOrEmpty(marketListingPageHtml))
            {
                var listingAssetMatchGroup = Regex.Match(marketListingPageHtml, "g_rgAssets\\s*=\\s*(.*);\r\n").Groups;
                var listingAsset = (listingAssetMatchGroup.Count > 1)
                    ? listingAssetMatchGroup[1].Value.Trim()
                    : null;

                if (!String.IsNullOrEmpty(listingAsset))
                {
                    try
                    {
                        // NOTE: This is a bit hacky, but the data we need is inside a JavaScript variable within a <script> element, so we try to parse the JSON value of the variable
                        var listingAssetDescription = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, Steam.Data.Models.Community.Models.SteamAssetDescription>>>>(listingAsset);
                        var listingAssetDescriptionHtml = listingAssetDescription?
                            .FirstOrDefault().Value?
                            .FirstOrDefault().Value?
                            .FirstOrDefault().Value?
                            .Descriptions?
                            .Where(x => String.Equals(x.Type, "html", StringComparison.InvariantCultureIgnoreCase))
                            .Select(x => x.Value)
                            .FirstOrDefault();
                        if (!String.IsNullOrEmpty(listingAssetDescriptionHtml))
                        {
                            // Strip HTML tags, just get the plain-text
                            asset.Description = Regex.Replace(listingAssetDescriptionHtml, "<[^>]*>", String.Empty).Trim();
                        }
                    }
                    catch (Exception)
                    {
                        // Likely because page says "no listings for item"
                        // The item probably isn't available on the community market
                    }
                }

                var itemNameIdMatchGroup = Regex.Match(marketListingPageHtml, "ItemActivityTicker.Start\\((.*)\\);\r\n").Groups;
                var itemNameId = (itemNameIdMatchGroup.Count > 1)
                    ? itemNameIdMatchGroup[1].Value.Trim()
                    : null;

                if (!String.IsNullOrEmpty(itemNameId))
                {
                    asset.NameId = UInt64.Parse(itemNameId);
                }
            }

            // Parse asset description from the store page (if available)
            if (storeItemPageHtml != null)
            {
                var descriptionHtml = storeItemPageHtml.Descendants("div").FirstOrDefault(x => x?.Attribute("class")?.Value == "item_description_snippet").Value;
                if (!String.IsNullOrEmpty(descriptionHtml))
                {
                    // Strip HTML tags, just get the plain-text
                    asset.Description = Regex.Replace(descriptionHtml, "<[^>]*>", String.Empty).Trim();
                }
            }

            // Parse asset crafting components from the description text (if available)
            if (!String.IsNullOrEmpty(asset.Description))
            {
                // Is this asset a crafting component?
                // e.g. "Cloth can be combined to craft"
                var craftingComponentMatchGroup = Regex.Match(asset.Description, "(.*) can be combined to craft").Groups;
                var craftingComponent = (craftingComponentMatchGroup.Count > 1)
                    ? craftingComponentMatchGroup[1].Value.Trim()
                    : null;
                if (!String.IsNullOrEmpty(craftingComponent))
                {
                    if (String.Equals(asset.Name, craftingComponent, StringComparison.InvariantCultureIgnoreCase))
                    {
                        asset.IsCraftingComponent = true;
                    }
                }

                // Is this asset able to be broken down in to crafting components?
                // e.g. "Breaks down into 1x Cloth"
                var breaksDownMatchGroup = Regex.Match(asset.Description, "Breaks down into (.*)").Groups;
                var breaksDown = (breaksDownMatchGroup.Count > 1)
                    ? breaksDownMatchGroup[1].Value.Trim()
                    : null;
                if (!String.IsNullOrEmpty(breaksDown))
                {
                    asset.Description = asset.Description.Replace(breaksDownMatchGroup[0].Value, String.Empty).Trim();
                    asset.IsBreakable = true;
                    asset.BreaksIntoComponents = new Steam.Data.Store.Types.PersistableAssetQuantityDictionary();

                    // e.g. "1x Cloth", "1x Wood", "1x Metal"
                    var componentMatches = Regex.Matches(breaksDown, "(\\d+)\\s*x\\s*(\\D*)").OfType<Match>();
                    foreach (var componentMatch in componentMatches)
                    {
                        var componentQuantity = componentMatch.Groups[1].Value;
                        var componentName = componentMatch.Groups[2].Value;
                        if (!String.IsNullOrEmpty(componentName))
                        {
                            asset.BreaksIntoComponents[componentName] = UInt32.Parse(componentQuantity);
                        }
                    }
                }

                // Is this asset a skin container and can it be sold on the market? If so, it is PROBABLY a craftable asset
                // e.g. "Barrels contain skins for weapons and tools."
                // e.g. "Bags contain clothes."
                // e.g. "Boxes contain deployables,"
                var isSkinContainer = Regex.IsMatch(asset.Description, "(.*)s contain (skins|weapons|tools|clothes|deployables)");
                if (isSkinContainer && asset.IsMarketable)
                {
                    asset.IsCraftable = true;
                }
            }

            // Parse asset tags (where missing)
            foreach (var tag in assetDescription?.Tags)
            {
                if (!asset.Tags.ContainsKey(tag.CategoryName))
                {
                    asset.Tags.Add(tag.CategoryName, tag.Name);
                }
            }
            if (publishedFile != null)
            {
                var workshopTag = publishedFile.Tags.FirstOrDefault(x => !Constants.SteamIgnoredWorkshopTags.Any(y => x == y));
                if (!String.IsNullOrEmpty(workshopTag))
                {
                    asset.Tags[Constants.SteamAssetTagWorkshop] = workshopTag;
                }
            }

            // Parse asset item "skin" tag (if missing)
            if (!asset.Tags.ContainsKey(Constants.SteamAssetTagSkin) && !String.IsNullOrEmpty(asset.Description))
            {
                // e.g. "This is a skin for the Large Wood Box item." 
                var skinMatchGroup = Regex.Match(asset.Description, "skin for the (.*) item\\.").Groups;
                var skin = (skinMatchGroup.Count > 1)
                    ? skinMatchGroup[1].Value.Trim()
                    : null;
                if (!String.IsNullOrEmpty(skin))
                {
                    asset.Tags[Constants.SteamAssetTagSkin] = skin;
                }
            }
            if (!asset.Tags.ContainsKey(Constants.SteamAssetTagSkin))
            {
                asset.Tags[Constants.SteamAssetTagSkin] = asset.Tags.GetItemType(asset.Name);
            }

            // Parse asset item "set" tag (if missing)
            if (!asset.Tags.ContainsKey(Constants.SteamAssetTagSet) && asset.Tags.Any())
            {
                // Remove all common item words from the collection name (e.g. "Box", "Pants", Door", etc)
                var itemCollection = assetDescription.Name;
                foreach (var tag in asset.Tags)
                {
                    foreach (var word in tag.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        itemCollection = itemCollection.Replace(word, String.Empty, StringComparison.InvariantCultureIgnoreCase);
                    }
                }
                foreach (var word in CommonItemWords)
                {
                    itemCollection = itemCollection.Replace(word, String.Empty, StringComparison.InvariantCultureIgnoreCase);
                }

                itemCollection = itemCollection.Trim();
                if (!String.IsNullOrEmpty(itemCollection))
                {
                    // Count the number of other assets that contain all the remaining unique collection words.
                    // If there is more than one, then it must be part of a set.
                    var query = _db.SteamAssetDescriptions.AsNoTracking();
                    foreach (var word in itemCollection.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        query = query.Where(x => x.Name.Contains(word));
                    }
                    if (await query.CountAsync() > 1)
                    {
                        asset.Tags[Constants.SteamAssetTagSet] = itemCollection;
                    }
                }
            }

            // Parse asset item type
            asset.ItemType = asset.Tags.GetItemType(asset.Name);

            return new ImportSteamAssetDescriptionResponse
            {
                Asset = asset
            };
        }
    }
}
