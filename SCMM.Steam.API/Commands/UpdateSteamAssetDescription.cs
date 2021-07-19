using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Models;
using SCMM.Steam.Data.Models.Community.Requests.Blob;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using Steam.Models;
using Steam.Models.SteamEconomy;
using SteamWebAPI2.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SCMM.Steam.API.Commands
{
    public class UpdateSteamAssetDescriptionRequest : ICommand<UpdateSteamAssetDescriptionResponse>
    {
        public SteamAssetDescription AssetDescription { get; set; }

        public AssetClassInfoModel AssetClass { get; set; }

        public PublishedFileDetailsModel PublishedFile { get; set; }

        public string MarketListingPageHtml { get; set; }

        public XElement StoreItemPageHtml { get; set; }
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
                assetDescription.BackgroundColour = assetClass.BackgroundColor?.SteamColourToWebHexString();
                assetDescription.ForegroundColour = assetClass.NameColor?.SteamColourToWebHexString();
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

                // Parse asset description (if any)
                if (assetClass.Descriptions != null && String.IsNullOrEmpty(assetDescription.Description))
                {
                    var itemDescription = assetClass.Descriptions
                        .Where(x =>
                            String.Equals(x.Type, Constants.SteamAssetClassDescriptionTypeHtml, StringComparison.InvariantCultureIgnoreCase) ||
                            String.Equals(x.Type, Constants.SteamAssetClassDescriptionTypeBBCode, StringComparison.InvariantCultureIgnoreCase)
                        )
                        .Select(x => x.Value)
                        .FirstOrDefault();

                    if (!String.IsNullOrEmpty(itemDescription))
                    {
                        // Strip any HTML and BBCode tags, just get the plain-text
                        itemDescription = Regex.Replace(itemDescription, Constants.SteamAssetClassDescriptionStripHtmlRegex, String.Empty).Trim();
                        itemDescription = Regex.Replace(itemDescription, Constants.SteamAssetClassDescriptionStripBBCodeRegex, String.Empty).Trim();
                        assetDescription.Description = itemDescription;
                    }
                }

                // Parse asset tags (if any)
                if (assetClass.Tags != null && assetDescription.Tags.Any())
                {
                    assetDescription.Tags = new PersistableStringDictionary(assetDescription.Tags);
                    foreach (var tag in assetClass.Tags)
                    {
                        assetDescription.Tags[tag.Category] = tag.Name;
                    }
                }
            }

            if (request.PublishedFile != null)
            {
                // Parse asset workshop details
                var publishedFile = request.PublishedFile;
                assetDescription.AssetType = SteamAssetDescriptionType.WorkshopItem;
                assetDescription.WorkshopFileId = publishedFile.PublishedFileId;
                assetDescription.NameWorkshop = publishedFile.Title;
                assetDescription.DescriptionWorkshop = publishedFile.Description;
                assetDescription.PreviewUrl = publishedFile.PreviewUrl?.ToString();
                assetDescription.PreviewContentId = publishedFile.PreviewContentHandle;
                assetDescription.CurrentSubscriptions = (long?)publishedFile.Subscriptions;
                assetDescription.LifetimeSubscriptions = (long?)publishedFile.LifetimeSubscriptions;
                assetDescription.CurrentFavourited = (long?)publishedFile.Favorited;
                assetDescription.LifetimeFavourited = (long?)publishedFile.LifetimeFavorited;
                assetDescription.Views = (long?)publishedFile.Views;
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
                            assetDescription.CreatorId = importedProfile.Profile.Id;
                        }
                    }
                    catch (Exception)
                    {
                        // Account has probably been deleted, not a big deal, continue on...
                    }
                }

                // Parse asset workshop tags (where missing)
                if (publishedFile.Tags != null)
                {
                    var interestingTags = publishedFile.Tags.Where(x => !Constants.SteamIgnoredWorkshopTags.Any(y => x == y));
                    if (interestingTags.Any())
                    {
                        assetDescription.Tags = new PersistableStringDictionary(assetDescription.Tags);
                        foreach (var tag in interestingTags)
                        {
                            var tagTrimmed = tag.Replace(" ", String.Empty).Trim();
                            var tagKey = $"{Constants.SteamAssetTagWorkshop}.{Char.ToLowerInvariant(tagTrimmed[0]) + tagTrimmed.Substring(1)}";
                            assetDescription.Tags[tagKey] = tag;
                        }
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

            // Parse asset icon and image data
            if (assetDescription.IconId == null && !String.IsNullOrEmpty(assetDescription.IconUrl))
            {
                try
                {
                    var importedImage = await _commandProcessor.ProcessWithResultAsync(new ImportImageDataRequest()
                    {
                        Url = assetDescription.IconUrl,
                        UseExisting = true
                    });
                    if (importedImage?.Image != null)
                    {
                        assetDescription.Icon = importedImage.Image;
                        assetDescription.IconId = importedImage.Image.Id;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Unable to import asset description icon. {ex.Message}");
                }
            }
            if (assetDescription.IconLargeId == null && !String.IsNullOrEmpty(assetDescription.IconLargeUrl))
            {
                try
                {
                    var importedImage = await _commandProcessor.ProcessWithResultAsync(new ImportImageDataRequest()
                    {
                        Url = assetDescription.IconLargeUrl,
                        UseExisting = true
                    });
                    if (importedImage?.Image != null)
                    {
                        assetDescription.IconLarge = importedImage.Image;
                        assetDescription.IconLargeId = importedImage.Image.Id;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Unable to import asset description large icon. {ex.Message}");
                }
            }
            if (assetDescription.PreviewId == null && !String.IsNullOrEmpty(assetDescription.PreviewUrl))
            {
                try
                {
                    var importedImage = await _commandProcessor.ProcessWithResultAsync(new ImportImageDataRequest()
                    {
                        Url = assetDescription.PreviewUrl,
                        UseExisting = true
                    });
                    if (importedImage?.Image != null)
                    {
                        assetDescription.Preview = importedImage.Image;
                        assetDescription.PreviewId = importedImage.Image.Id;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Unable to import asset description preview image. {ex.Message}");
                }
            }

            // Parse asset description and name id from the market list page (if available)
            if (!String.IsNullOrEmpty(request.MarketListingPageHtml))
            {
                if (String.IsNullOrEmpty(assetDescription.Description))
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
                                // Strip any HTML tags, just get the plain-text
                                assetDescription.Description = Regex.Replace(itemDescriptionHtml, Constants.SteamAssetClassDescriptionStripHtmlRegex, String.Empty).Trim();
                            }
                        }
                        catch (Exception)
                        {
                            // Likely because page says "no listings for item"
                            // The item probably isn't available on the community market
                        }
                    }
                }
                if (assetDescription.NameId == null)
                {
                    var itemNameIdMatchGroup = Regex.Match(request.MarketListingPageHtml, Constants.SteamMarketListingItemNameIdRegex).Groups;
                    var itemNameId = (itemNameIdMatchGroup.Count > 1)
                        ? itemNameIdMatchGroup[1].Value.Trim()
                        : null;

                    if (!String.IsNullOrEmpty(itemNameId))
                    {
                        assetDescription.NameId = UInt64.Parse(itemNameId);
                    }
                }
            }

            // Parse asset description from the store page (if available)
            if (request.StoreItemPageHtml != null)
            {
                if (String.IsNullOrEmpty(assetDescription.Description))
                {
                    var itemDescriptionHtml = request.StoreItemPageHtml.Descendants("div").FirstOrDefault(x => x?.Attribute("class")?.Value == Constants.SteamStoreItemDescriptionName).Value;
                    if (!String.IsNullOrEmpty(itemDescriptionHtml))
                    {
                        // Strip any HTML tags, just get the plain-text
                        assetDescription.Description = Regex.Replace(itemDescriptionHtml, Constants.SteamAssetClassDescriptionStripHtmlRegex, String.Empty).Trim();
                    }
                }
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
                    assetDescription.BreaksIntoComponents = new PersistableAssetQuantityDictionary();

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
            if (String.IsNullOrEmpty(assetDescription.ItemType))
            {
                if (!String.IsNullOrEmpty(assetDescription.Description))
                {
                    // e.g. "This is a skin for the Large Wood Box item." 
                    var itemTypeMatchGroup = Regex.Match(assetDescription.Description, @"skin for the (.*) item\.").Groups;
                    var itemType = (itemTypeMatchGroup.Count > 1)
                        ? itemTypeMatchGroup[1].Value.Trim()
                        : null;

                    // Is it an item skin?
                    if (!String.IsNullOrEmpty(itemType))
                    {
                        assetDescription.ItemType = itemType;
                    }
                    // Is it a crafting component?
                    else if (assetDescription.IsCraftingComponent)
                    {
                        assetDescription.ItemType = Constants.RustItemTypeResource;
                    }
                    // Is it a craftable container?
                    else if (assetDescription.IsCraftable)
                    {
                        assetDescription.ItemType = Constants.RustItemTypeContainer;
                    }
                    // Is it a non-craftable container?
                    // e.g. "This special crate acquired from a twitch drop during Trust in Rust 3 will yield a random skin"
                    else if (Regex.IsMatch(assetDescription.Description, @"crate .* random skin"))
                    {
                        assetDescription.ItemType = Constants.RustItemTypeContainer;
                    }
                    // Is it a unique item?
                    // e.g. "Having this item in your Steam Inventory means you'll be able to craft it in game. If you sell, trade or break this item you will no longer have this ability in game."
                    else if (Regex.IsMatch(assetDescription.Description, @"craft it in game"))
                    {
                        assetDescription.ItemType = Constants.RustItemTypeUnique;
                    }
                    // Is it an underwear item?
                    // e.g. "Having this item in your Steam Inventory means you'll be able to select this as your players default appearance. If you sell, trade or break this item you will no longer have this ability in game."
                    else if (Regex.IsMatch(assetDescription.Description, @"players default appearance"))
                    {
                        assetDescription.ItemType = Constants.RustItemTypeUnderwear;
                    }
                }
                else
                {
                    // HACK: Facepunch messed up the LR300 skins. The item descriptions are always empty so try fill in the blanks
                    if (assetDescription.Tags.Any(x => x.Value == Constants.RustItemTypeLR300))
                    {
                        assetDescription.ItemType = Constants.RustItemTypeLR300;
                    }
                }
            }

            // Parse asset item collection (if missing and is a user created item)
            if (String.IsNullOrEmpty(assetDescription.ItemCollection) && assetDescription.CreatorId != null)
            {
                // Find existing item collections we fit in to (if any)
                var existingItemCollections = await _db.SteamAssetDescriptions
                    .Where(x => x.CreatorId == assetDescription.CreatorId)
                    .Where(x => !String.IsNullOrEmpty(x.ItemCollection))
                    .Select(x => x.ItemCollection)
                    .Distinct()
                    .ToListAsync();
                if (existingItemCollections.Any())
                {
                    foreach (var existingItemCollection in existingItemCollections.OrderByDescending(x => x.Length))
                    {
                        var isCollectionMatch = existingItemCollection
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .All(x => assetDescription.Name.Contains(x));
                        if (isCollectionMatch)
                        {
                            assetDescription.ItemCollection = existingItemCollection;
                            break;
                        }
                    }
                }

                // If we don't have an item collection at this point, then there are no existing collections that we fit into  :(
                // Find other skins similar to us and see if we can start a new item collection, with black jack and hookers...
                if (String.IsNullOrEmpty(assetDescription.ItemCollection))
                {
                    // Remove all common item words from the collection name (e.g. "Box", "Pants", Door", etc)
                    // NOTE: Pattern match word boundarys to prevent replacing words within words.
                    //       e.g. "Stone" in "Stonecraft Hatchet" shouldn't end up like "craft Hatchet"
                    var newItemCollection = assetDescription.Name;
                    if (!String.IsNullOrEmpty(assetDescription.ItemType))
                    {
                        foreach (var word in assetDescription.ItemType.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            newItemCollection = Regex.Replace(newItemCollection, $@"\b{word}\b", String.Empty, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                        }
                    }
                    foreach (var tag in assetDescription.Tags)
                    {
                        foreach (var word in tag.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            newItemCollection = Regex.Replace(newItemCollection, $@"\b{word}\b", String.Empty, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                        }
                    }
                    foreach (var word in Constants.RustItemNameCommonWords)
                    {
                        newItemCollection = Regex.Replace(newItemCollection, $@"\b{word}\b", String.Empty, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                    }

                    // Ensure all remaining words are longer than one character, otherwise strip them out.
                    // This fixes scenarios like "Satchelo" => "o", "Rainbow Doors" => "Rainbow s", etc
                    newItemCollection = Regex.Replace(newItemCollection, @"\b(\w{1,2})\b", String.Empty, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

                    // Trim any junk characters
                    newItemCollection = newItemCollection.Trim(' ', ',', '.', '-', '\'', ':').Trim();

                    // If there is anything left, we have a unique collection name, try find others with the same name
                    if (!String.IsNullOrEmpty(newItemCollection))
                    {
                        // Count the number of other assets created by the same author that also contain the remaining unique collection words.
                        // If there is more than one item, then it must be part of a set.
                        var query = _db.SteamAssetDescriptions
                            .Where(x => x.CreatorId == assetDescription.CreatorId)
                            .Where(x => String.IsNullOrEmpty(x.ItemCollection));
                        foreach (var word in newItemCollection.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            query = query.Where(x => x.Name.Contains(word));
                        }
                        var collectionItems = await query.ToListAsync();
                        if (collectionItems.Count > 1)
                        {
                            // Update all items in the collection (this helps fix old items when they become part of a new collection)
                            foreach (var item in collectionItems)
                            {
                                item.ItemCollection = newItemCollection;
                            }
                        }
                    }
                }
            }

            // Parse name and description to determine if this item glows
            if (!assetDescription.Tags.ContainsKey(Constants.RustAssetTagGlow))
            {
                var glowing = false;
                var descriptionText = String.Join(' ',
                    assetDescription.Name, assetDescription.NameWorkshop, assetDescription.Description, assetDescription.DescriptionWorkshop
                );

                // Ignore certain item types that are expected to glow or only glow conditionally (e.g. furances)
                var nonGlowingItemTypes = new string[] { "Furnace" };
                if (!nonGlowingItemTypes.Contains(assetDescription.ItemType))
                {
                    // Check that phrases like "no glow", "not glowing", "doesn't glow", "don't glow", "non-glow", etc don't appear anywhere in the description. If they do, then it probably doesn't glow
                    if (!Regex.IsMatch(descriptionText, @"\bno[t]*\b[^\.\n]*\bglow", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase) &&
                        !Regex.IsMatch(descriptionText, @"\bdo[es]*n't*\b[^\.\n]*\bglow", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase) &&
                        !Regex.IsMatch(descriptionText, @"\bnon[-]*glow", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
                    {
                        // Now check if the words "glow" or "glowing" appear. If so, then it is probably a glowing item
                        if (Regex.IsMatch(descriptionText, @"\bglow[ing]*\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
                        {
                            glowing = true;
                        }
                    }
                }

                assetDescription.Tags = new PersistableStringDictionary(assetDescription.Tags);
                assetDescription.Tags.SetFlag(Constants.RustAssetTagGlow, glowing);
            }

            // Check if this is a twitch drop item
            if (!assetDescription.IsTwitchDrop)
            {
                // Does this look like a streamer commissioned twitch drop?
                if (assetDescription.WorkshopFileId != null && assetDescription.TimeCreated > new DateTime(2020, 01, 01) && !assetDescription.IsMarketable && (assetDescription.LifetimeSubscriptions == null || assetDescription.LifetimeSubscriptions == 0))
                {
                    assetDescription.IsTwitchDrop = true;
                }
                // Does this look like a publisher item twitch drop?
                else if (assetDescription.WorkshopFileId == null && Regex.IsMatch(assetDescription.Description, @"Twitch", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
                {
                    assetDescription.IsTwitchDrop = true;
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
