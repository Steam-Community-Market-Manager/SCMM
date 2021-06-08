using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SteamWebAPI2.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using SCMM.Steam.Data.Models.Community.Requests.Blob;
using Newtonsoft.Json;
using Steam.Models.SteamEconomy;
using Steam.Models;
using SCMM.Steam.Data.Models.Community.Models;

namespace SCMM.Steam.API.Commands
{
    public class UpdateSteamAssetDescriptionRequest : ICommand<UpdateSteamAssetDescriptionResponse>
    {
        public SteamAssetDescription AssetDescription { get; set; }

        public AssetClassInfoModel AssetClass { get; set; }

        public PublishedFileDetailsModel PublishedFile { get; set; }

        public string MarketListingPageHtml { get; set; }

        public XElement StoreItemPageHtml { get; set; }

        public string ItemDescription { get; set; }
    }

    public class UpdateSteamAssetDescriptionResponse
    {
        public SteamAssetDescription AssetDescription { get; set; }
    }

    public class UpdateSteamAssetDescription : ICommandHandler<UpdateSteamAssetDescriptionRequest, UpdateSteamAssetDescriptionResponse>
    {
        private readonly ILogger<UpdateSteamAssetDescription> _logger;
        private readonly SteamDbContext _db;
        private readonly SteamConfiguration _cfg;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public UpdateSteamAssetDescription(ILogger<UpdateSteamAssetDescription> logger, SteamDbContext db, IConfiguration cfg, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _logger = logger;
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        public async Task<UpdateSteamAssetDescriptionResponse> HandleAsync(UpdateSteamAssetDescriptionRequest request)
        {
            var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_cfg.ApplicationKey);
            var assetDescription = request.AssetDescription;
            if (assetDescription == null)
            {
                throw new ArgumentNullException(nameof(request.AssetDescription));
            }

            if (request.AssetClass != null)
            {
                // Parse asset description details
                var assetClass = request.AssetClass;
                assetDescription.Name = assetClass.MarketName;
                assetDescription.NameHash = assetClass.MarketHashName;
                assetDescription.IconUrl = new SteamEconomyImageBlobRequest(assetClass.IconUrl);
                assetDescription.IconLargeUrl = new SteamEconomyImageBlobRequest(assetClass.IconUrlLarge ?? assetClass.IconUrl);
                assetDescription.BackgroundColour = assetClass.BackgroundColor?.SteamColourToHexString();
                assetDescription.ForegroundColour = assetClass.NameColor?.SteamColourToHexString();
                assetDescription.IsCommodity = String.Equals(assetClass.Commodity, "1", StringComparison.InvariantCultureIgnoreCase);
                assetDescription.IsMarketable = String.Equals(assetClass.Marketable, "1", StringComparison.InvariantCultureIgnoreCase);
                assetDescription.MarketableRestrictionDays = String.IsNullOrEmpty(assetClass.MarketMarketableRestriction) ? (int?)null : Int32.Parse(assetClass.MarketMarketableRestriction);
                assetDescription.IsTradable = String.Equals(assetClass.Tradable, "1", StringComparison.InvariantCultureIgnoreCase);
                assetDescription.TradableRestrictionDays = String.IsNullOrEmpty(assetClass.MarketTradableRestriction) ? (int?)null : Int32.Parse(assetClass.MarketTradableRestriction);

                // Parse asset type
                switch (assetClass.Type)
                {
                    case Constants.SteamAssetClassTypeWorkshopItem: assetDescription.AssetType = SteamAssetDescriptionType.WorkshopItem; break;
                }

                // Parse asset tags (where missing)
                foreach (var tag in assetClass?.Tags)
                {
                    if (!assetDescription.Tags.ContainsKey(tag.Category))
                    {
                        assetDescription.Tags.Add(tag.Category, tag.Name);
                    }
                }
            }

            if (request.PublishedFile != null)
            {
                // Parse asset workshop details
                var publishedFile = request.PublishedFile;
                assetDescription.AssetType = SteamAssetDescriptionType.WorkshopItem;
                assetDescription.WorkshopFileId = publishedFile.PublishedFileId;
                assetDescription.ImageUrl = publishedFile.PreviewUrl?.ToString();
                assetDescription.CurrentSubscriptions = (long?)publishedFile.Subscriptions;
                assetDescription.TotalSubscriptions = (long?)publishedFile.LifetimeSubscriptions;
                assetDescription.IsBanned = publishedFile.Banned;
                assetDescription.BanReason = publishedFile.BanReason;
                assetDescription.TimeCreated = publishedFile.TimeCreated > DateTime.MinValue ? publishedFile.TimeCreated : null;
                assetDescription.TimeUpdated = publishedFile.TimeUpdated > DateTime.MinValue ? publishedFile.TimeUpdated : null;

                // Parse asset workshop creator
                if (assetDescription.CreatorId == null && publishedFile.Creator > 0)
                {
                    try
                    {
                        var importedProfile = await _commandProcessor.ProcessWithResultAsync(
                            new ImportSteamProfileRequest()
                            {
                                ProfileId = publishedFile.Creator.ToString()
                            }
                        );
                        if (importedProfile?.Profile != null)
                        {
                            assetDescription.Creator = importedProfile.Profile;
                        }
                    }
                    catch (Exception)
                    {
                        // Account has probably been deleted, not a big deal, continue on...
                    }
                }

                // Parse asset workshop tags (where missing)
                foreach (var tag in publishedFile.Tags.Where(x => !Constants.SteamIgnoredWorkshopTags.Any(y => x == y)))
                {
                    var tagTrimmed = tag.Replace(" ", String.Empty).Trim();
                    var tagKey = $"{Constants.SteamAssetTagWorkshop}.{Char.ToLowerInvariant(tagTrimmed[0]) + tagTrimmed.Substring(1)}";
                    if (!assetDescription.Tags.ContainsKey(tagKey))
                    {
                        assetDescription.Tags[tagKey] = tag;
                    }
                }

                // Update asset subscription history graph data
                // TODO: Revive this, or remove it
                /*
                if (UpdateSubscriptionGraph)
                {
                    var utcDate = DateTime.UtcNow.Date;
                    var maxSubscriptions = assetDescription.TotalSubscriptions;
                    if (assetDescription.SubscriptionsGraph.ContainsKey(utcDate))
                    {
                        maxSubscriptions = (int)Math.Max(maxSubscriptions, assetDescription.SubscriptionsGraph[utcDate]);
                    }
                    assetDescription.SubscriptionsGraph[utcDate] = maxSubscriptions;
                    assetDescription.SubscriptionsGraph = new PersistableDailyGraphDataSet(
                        assetDescription.SubscriptionsGraph
                    );
                }
                */
            }

            // Parse asset app
            //if (assetDescription.AppId == Guid.Empty)
            //{
            //    assetDescription.App = await _db.SteamApps.FirstOrDefaultAsync(x => x.SteamId == request.AppId.ToString());
            //}

            // Parse asset icon and image data
            if (assetDescription.IconId == null && !String.IsNullOrEmpty(assetDescription.IconUrl))
            {
                var importedImage = await _commandProcessor.ProcessWithResultAsync(new ImportImageDataRequest()
                {
                    Url = assetDescription.IconUrl,
                    UseExisting = true
                });
                if (importedImage?.Image != null)
                {
                    assetDescription.Icon = importedImage.Image;
                }
            }
            if (assetDescription.IconLargeId == null && !String.IsNullOrEmpty(assetDescription.IconLargeUrl))
            {
                var importedImage = await _commandProcessor.ProcessWithResultAsync(new ImportImageDataRequest()
                {
                    Url = assetDescription.IconLargeUrl,
                    UseExisting = true
                });
                if (importedImage?.Image != null)
                {
                    assetDescription.IconLarge = importedImage.Image;
                }
            }
            if (assetDescription.ImageId == null && !String.IsNullOrEmpty(assetDescription.ImageUrl))
            {
                var importedImage = await _commandProcessor.ProcessWithResultAsync(new ImportImageDataRequest()
                {
                    Url = assetDescription.ImageUrl,
                    UseExisting = true
                });
                if (importedImage?.Image != null)
                {
                    assetDescription.Image = importedImage.Image;
                }
            }

            // Parse asset description and name id from the market list page (if available)
            if (!String.IsNullOrEmpty(request.MarketListingPageHtml))
            {
                var listingAssetMatchGroup = Regex.Match(request.MarketListingPageHtml, Constants.SteamMarketListingAssetJsonRegex).Groups;
                var listingAssetJson = (listingAssetMatchGroup.Count > 1)
                    ? listingAssetMatchGroup[1].Value.Trim()
                    : null;

                if (!String.IsNullOrEmpty(listingAssetJson))
                {
                    try
                    {
                        // NOTE: This is a bit hacky, but the data we need is inside a JavaScript variable within a <script> element, so we try to parse the JSON value of the variable
                        var listingAsset = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, SteamAssetClass>>>>(listingAssetJson);
                        var itemDescriptionHtml = listingAsset?
                            .FirstOrDefault().Value?
                            .FirstOrDefault().Value?
                            .FirstOrDefault().Value?
                            .Descriptions?
                            .Where(x => String.Equals(x.Type, Constants.SteamAssetClassDescriptionTypeHtml, StringComparison.InvariantCultureIgnoreCase))
                            .Select(x => x.Value)
                            .FirstOrDefault();
                        if (!String.IsNullOrEmpty(itemDescriptionHtml))
                        {
                            // Strip HTML tags, just get the plain-text
                            assetDescription.Description = Regex.Replace(itemDescriptionHtml, Constants.SteamAssetClassDescriptionStripHtmlRegex, String.Empty).Trim();
                        }
                    }
                    catch (Exception)
                    {
                        // Likely because page says "no listings for item"
                        // The item probably isn't available on the community market
                    }
                }

                var itemNameIdMatchGroup = Regex.Match(request.MarketListingPageHtml, Constants.SteamMarketListingItemNameIdRegex).Groups;
                var itemNameId = (itemNameIdMatchGroup.Count > 1)
                    ? itemNameIdMatchGroup[1].Value.Trim()
                    : null;

                if (!String.IsNullOrEmpty(itemNameId))
                {
                    assetDescription.NameId = UInt64.Parse(itemNameId);
                }
            }

            // Parse asset description from the store page (if available)
            if (request.StoreItemPageHtml != null)
            {
                var itemDescriptionHtml = request.StoreItemPageHtml.Descendants("div").FirstOrDefault(x => x?.Attribute("class")?.Value == Constants.SteamStoreItemDescriptionName).Value;
                if (!String.IsNullOrEmpty(itemDescriptionHtml))
                {
                    // Strip HTML tags, just get the plain-text
                    assetDescription.Description = Regex.Replace(itemDescriptionHtml, Constants.SteamAssetClassDescriptionStripHtmlRegex, String.Empty).Trim();
                }
            }

            // Parse asset description from the item description html (if available)
            if (!String.IsNullOrEmpty(request.ItemDescription))
            {
                // Strip HTML and BBCode tags, just get the plain-text
                var itemDescription = request.ItemDescription;
                itemDescription = Regex.Replace(request.ItemDescription, Constants.SteamAssetClassDescriptionStripHtmlRegex, String.Empty).Trim();
                itemDescription = Regex.Replace(request.ItemDescription, Constants.SteamAssetClassDescriptionStripBBCodeRegex, String.Empty).Trim();
                assetDescription.Description = itemDescription;
            }

            // Parse asset crafting components from the description text (if available)
            if (!String.IsNullOrEmpty(assetDescription.Description))
            {
                // Is this asset a crafting component?
                // e.g. "Cloth can be combined to craft"
                var craftingComponentMatchGroup = Regex.Match(assetDescription.Description, @"(.*) can be combined to craft").Groups;
                var craftingComponent = (craftingComponentMatchGroup.Count > 1)
                    ? craftingComponentMatchGroup[1].Value.Trim()
                    : null;
                if (!String.IsNullOrEmpty(craftingComponent))
                {
                    if (String.Equals(assetDescription.Name, craftingComponent, StringComparison.InvariantCultureIgnoreCase))
                    {
                        assetDescription.IsCraftingComponent = true;
                    }
                }

                // Is this asset able to be broken down in to crafting components?
                // e.g. "Breaks down into 1x Cloth"
                var breaksDownMatchGroup = Regex.Match(assetDescription.Description, @"Breaks down into (.*)").Groups;
                var breaksDown = (breaksDownMatchGroup.Count > 1)
                    ? breaksDownMatchGroup[1].Value.Trim()
                    : null;
                if (!String.IsNullOrEmpty(breaksDown))
                {
                    assetDescription.Description = assetDescription.Description.Replace(breaksDownMatchGroup[0].Value, String.Empty).Trim();
                    assetDescription.IsBreakable = true;
                    assetDescription.BreaksIntoComponents = new Steam.Data.Store.Types.PersistableAssetQuantityDictionary();

                    // e.g. "1x Cloth", "1x Wood", "1x Metal"
                    var componentMatches = Regex.Matches(breaksDown, @"(\d+)\s*x\s*(\D*)").OfType<Match>();
                    foreach (var componentMatch in componentMatches)
                    {
                        var componentQuantity = componentMatch.Groups[1].Value;
                        var componentName = componentMatch.Groups[2].Value;
                        if (!String.IsNullOrEmpty(componentName))
                        {
                            assetDescription.BreaksIntoComponents[componentName] = UInt32.Parse(componentQuantity);
                        }
                    }
                }

                // Is this asset a skin container and can it be sold on the market? If so, it is PROBABLY a craftable asset
                // e.g. "Barrels contain skins for weapons and tools."
                // e.g. "Bags contain clothes."
                // e.g. "Boxes contain deployables,"
                var isSkinContainer = Regex.IsMatch(assetDescription.Description, @"(.*)s contain (skins|weapons|tools|clothes|deployables)");
                if (isSkinContainer && assetDescription.IsMarketable)
                {
                    assetDescription.IsCraftable = true;
                }
            }

            // Parse asset item type (if missing)
            if (String.IsNullOrEmpty(assetDescription.ItemType) && !String.IsNullOrEmpty(assetDescription.Description))
            {
                // e.g. "This is a skin for the Large Wood Box item" 
                var itemTypeMatchGroup = Regex.Match(assetDescription.Description, @"skin for the (.*) item").Groups;
                var itemType = (itemTypeMatchGroup.Count > 1)
                    ? itemTypeMatchGroup[1].Value.Trim()
                    : null;
                if (!String.IsNullOrEmpty(itemType))
                {
                    assetDescription.ItemType = itemType;
                }
            }

            // Parse asset item collection (if missing)
            if (String.IsNullOrEmpty(assetDescription.ItemCollection) && assetDescription.Tags.Any())
            {
                // Remove all common item words from the collection name (e.g. "Box", "Pants", Door", etc)
                var itemCollection = assetDescription.Name;
                foreach (var tag in assetDescription.Tags)
                {
                    foreach (var word in tag.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        itemCollection = itemCollection.Replace(word, String.Empty, StringComparison.InvariantCultureIgnoreCase);
                    }
                }
                foreach (var word in Constants.SteamItemNameCommonWords)
                {
                    itemCollection = itemCollection.Replace(word, String.Empty, StringComparison.InvariantCultureIgnoreCase);
                }

                itemCollection = itemCollection.Trim();
                if (!String.IsNullOrEmpty(itemCollection))
                {
                    // Count the number of other assets created by the same author that also contain the remaining unique collection words.
                    // If there is more than one item, then it must be part of a set.
                    var query = _db.SteamAssetDescriptions.Where(x => x.CreatorId == assetDescription.CreatorId);
                    foreach (var word in itemCollection.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        query = query.Where(x => x.Name.Contains(word));
                    }
                    var collectionItems = await query.ToListAsync();
                    if (collectionItems.Count > 1)
                    {
                        // Update all items in the collection (this helps fix old items when they become part of a new collection)
                        foreach (var item in collectionItems)
                        {
                            item.ItemCollection = itemCollection;
                        }
                    }
                }
            }

            // Update last checked on
            assetDescription.TimeRefreshed = DateTimeOffset.Now;

            return new UpdateSteamAssetDescriptionResponse
            {
                AssetDescription = assetDescription
            };
        }
    }
}
