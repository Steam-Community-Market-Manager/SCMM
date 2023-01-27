using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Models;
using SCMM.Steam.Data.Models.Community.Requests.Blob;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Models.WebApi.Models;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using Steam.Models;
using Steam.Models.SteamEconomy;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SCMM.Steam.API.Commands
{
    public class UpdateSteamAssetDescriptionRequest : ICommand<UpdateSteamAssetDescriptionResponse>
    {
        public SteamAssetDescription AssetDescription { get; set; }

        public ItemDefinition AssetItemDefinition { get; set; }

        public AssetClassInfoModel AssetClass { get; set; }

        public PublishedFileDetailsModel PublishedFile { get; set; }

        public PublishedFileVoteData PublishedFileVoteData { get; set; }

        public IEnumerable<PublishedFilePreview> PublishedFilePreviews { get; set; }

        public XElement PublishedFileChangeNotesPageHtml { get; set; }

        public string MarketListingPageHtml { get; set; }

        public XElement StoreItemPageHtml { get; set; }

        /// <summary>
        /// If true, the item collection name check will be skipped
        /// </summary>
        public bool SkipItemCollectionCheck { get; set; } = false;
    }

    public class UpdateSteamAssetDescriptionResponse
    {
        public SteamAssetDescription AssetDescription { get; set; }
    }

    public class UpdateSteamAssetDescription : ICommandHandler<UpdateSteamAssetDescriptionRequest, UpdateSteamAssetDescriptionResponse>
    {
        private readonly ILogger<UpdateSteamAssetDescription> _logger;
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IServiceBus _serviceBus;

        public UpdateSteamAssetDescription(ILogger<UpdateSteamAssetDescription> logger, SteamDbContext db, IConfiguration cfg, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, IServiceBus serviceBus)
        {
            _logger = logger;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _serviceBus = serviceBus;
        }

        public async Task<UpdateSteamAssetDescriptionResponse> HandleAsync(UpdateSteamAssetDescriptionRequest request)
        {
            var assetDescription = request.AssetDescription;
            if (assetDescription == null)
            {
                throw new ArgumentNullException(nameof(request.AssetDescription));
            }

            var app = (request.AssetDescription.App);
            var appId = (request.AssetDescription.App != null ? (ulong?)UInt64.Parse(request.AssetDescription.App.SteamId) : null) ??
                        (request.AssetItemDefinition != null ? (ulong?)request.AssetItemDefinition.AppId : null) ??
                        (request.PublishedFile != null ? (ulong?)request.PublishedFile.ConsumerAppId : null);

            // Parse asset item definition details
            if (request.AssetItemDefinition != null)
            {
                var itemDefinition = request.AssetItemDefinition;
                assetDescription.WorkshopFileId = (assetDescription.WorkshopFileId == null && itemDefinition.WorkshopId > 0) ? itemDefinition.WorkshopId : assetDescription.WorkshopFileId;
                assetDescription.ItemDefinitionId = itemDefinition.ItemDefId;
                assetDescription.ItemType = ((String.IsNullOrEmpty(assetDescription.ItemType) && !String.IsNullOrEmpty(itemDefinition.DisplayType)) ? itemDefinition.DisplayType : assetDescription.ItemType);
                assetDescription.Name = (String.IsNullOrEmpty(assetDescription.Name) ? (itemDefinition.Name ?? itemDefinition.MarketName) : assetDescription.Name);
                assetDescription.NameHash = (String.IsNullOrEmpty(assetDescription.NameHash) ? itemDefinition.MarketHashName ?? itemDefinition.Name : assetDescription.NameHash ?? assetDescription.Name);
                assetDescription.IconUrl = (String.IsNullOrEmpty(assetDescription.IconUrl) && !String.IsNullOrEmpty(itemDefinition.IconUrl)) ? new SteamBlobRequest(itemDefinition.IconUrl) : assetDescription.IconUrl;
                assetDescription.IconLargeUrl = (String.IsNullOrEmpty(assetDescription.IconLargeUrl) && !String.IsNullOrEmpty(itemDefinition.IconUrlLarge)) ? new SteamBlobRequest(itemDefinition.IconUrlLarge ?? itemDefinition.IconUrl) : assetDescription.IconLargeUrl;
                assetDescription.BackgroundColour = (String.IsNullOrEmpty(assetDescription.BackgroundColour) ? itemDefinition.BackgroundColor?.SteamColourToWebHexString() : assetDescription.BackgroundColour);
                assetDescription.ForegroundColour = (String.IsNullOrEmpty(assetDescription.ForegroundColour) ? itemDefinition.NameColor?.SteamColourToWebHexString() : assetDescription.ForegroundColour);
                assetDescription.IsMarketable = itemDefinition.Marketable;
                assetDescription.IsTradable = itemDefinition.Tradable;
                assetDescription.IsCommodity = itemDefinition.Commodity;
                assetDescription.IsAccepted = true;
                assetDescription.TimeAccepted = (assetDescription.TimeAccepted ?? itemDefinition.DateCreated.SteamTimestampToDateTimeOffset());
                // NOTE: 'DateCreated' seems to reset in the item defs everytime the item is modified, so we always use the earliest known date.
                assetDescription.TimeCreated = assetDescription.TimeCreated.Earliest(itemDefinition.DateCreated.SteamTimestampToDateTimeOffset());
                assetDescription.TimeUpdated = assetDescription.TimeUpdated.Latest(itemDefinition.Modified.SteamTimestampToDateTimeOffset());

                // Parse item type
                var itemDefinitionType = SteamItemDefinitionType.Item;
                if (Enum.TryParse(itemDefinition.Type, true, out itemDefinitionType))
                {
                    assetDescription.ItemDefinitionType = itemDefinitionType;
                    assetDescription.ItemType = ((String.IsNullOrEmpty(assetDescription.ItemType) && !String.IsNullOrEmpty(itemDefinition.DisplayType)) ? itemDefinition.DisplayType : assetDescription.ItemType);
                }
                else
                {
                    assetDescription.ItemDefinitionType = SteamItemDefinitionType.Item;
                    assetDescription.ItemType = ((String.IsNullOrEmpty(assetDescription.ItemType) && !String.IsNullOrEmpty(itemDefinition.Type)) ? itemDefinition.Type : assetDescription.ItemType);
                }

                // Parse asset description (if any)
                if (!string.IsNullOrEmpty(itemDefinition.Description))
                {
                    // Strip any HTML and BBCode tags, just get the plain-text
                    assetDescription.Description = itemDefinition.Description.ToSafeMarkup().Trim();
                }

                // Parse asset tags (if any)
                if (itemDefinition.Tags != null)
                {
                    assetDescription.Tags = new PersistableStringDictionary(assetDescription.Tags);
                    foreach (var tag in itemDefinition.Tags.Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    {
                        var tagKeyValuePair = tag.Split(":", StringSplitOptions.TrimEntries);
                        if (tagKeyValuePair.Length > 1)
                        {
                            assetDescription.Tags[tagKeyValuePair.FirstOrDefault()] = tagKeyValuePair.LastOrDefault();
                        }
                    }
                }

                // Parse asset store tags
                if (itemDefinition.StoreTags != null)
                {
                    var existingNonStoreTags = assetDescription.Tags.Where(x => !x.Key.StartsWith(Constants.SteamAssetTagStore)).ToDictionary(x => x.Key, x => x.Value);
                    assetDescription.Tags = new PersistableStringDictionary(existingNonStoreTags);
                    foreach (var tag in itemDefinition.StoreTags.Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    {
                        var tagKey = $"{Constants.SteamAssetTagStore}.{tag}";
                        assetDescription.Tags[tagKey] = tag;
                    }
                }

                // Parse bundle
                if (itemDefinition.Bundle != null)
                {
                    assetDescription.Bundle = new PersistableItemBundleDictionary();
                    foreach (var item in itemDefinition.Bundle.Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    {
                        var bundleItemParts = item.Split("x", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        var bundleItemId = ulong.Parse(bundleItemParts.FirstOrDefault());
                        var bundleItemWeight = bundleItemParts.Length > 1 ? uint.Parse(bundleItemParts.Skip(1).FirstOrDefault()) : 1;
                        assetDescription.Bundle[bundleItemId] = bundleItemWeight;
                    }
                }

                // TODO: Store tags "nocrate"

                // TODO: Exchange (https://partner.steamgames.com/doc/features/inventory/schema#ExchangeFormat)
                // TODO: Promo (https://partner.steamgames.com/doc/features/inventory/schema#PromoItems)
                // TODO: Price Category (https://partner.steamgames.com/doc/features/inventory/schema#SpecifyPrices)

            }

            // Parse asset description details
            if (request.AssetClass != null)
            {
                var assetClass = request.AssetClass;
                assetDescription.ClassId = assetClass.ClassId;
                assetDescription.ItemType = ((String.IsNullOrEmpty(assetDescription.ItemType) && !String.IsNullOrEmpty(assetClass.Type)) ? assetClass.Type : assetDescription.ItemType);
                assetDescription.Name = assetClass.MarketName;
                assetDescription.NameHash = assetClass.MarketHashName ?? assetClass.Name;
                assetDescription.IconUrl = !string.IsNullOrEmpty(assetClass.IconUrl) ? new SteamEconomyImageBlobRequest(assetClass.IconUrl) : null;
                assetDescription.IconLargeUrl = !string.IsNullOrEmpty(assetClass.IconUrlLarge) ? new SteamEconomyImageBlobRequest(assetClass.IconUrlLarge ?? assetClass.IconUrl) : null;
                assetDescription.BackgroundColour = assetClass.BackgroundColor?.SteamColourToWebHexString();
                assetDescription.ForegroundColour = assetClass.NameColor?.SteamColourToWebHexString();
                assetDescription.MarketableRestrictionDays = string.IsNullOrEmpty(assetClass.MarketMarketableRestriction) ? (int?)null : int.Parse(assetClass.MarketMarketableRestriction);
                assetDescription.IsMarketable = (
                    string.Equals(assetClass.Marketable, "1", StringComparison.InvariantCultureIgnoreCase) ||
                    (string.IsNullOrEmpty(assetClass.Marketable) && !string.IsNullOrEmpty(assetClass.MarketHashName)) ||
                    assetDescription.MarketableRestrictionDays != null
                );
                assetDescription.TradableRestrictionDays = string.IsNullOrEmpty(assetClass.MarketTradableRestriction) ? (int?)null : int.Parse(assetClass.MarketTradableRestriction);
                assetDescription.IsTradable = (
                    string.Equals(assetClass.Tradable, "1", StringComparison.InvariantCultureIgnoreCase) ||
                    assetDescription.TradableRestrictionDays != null
                );
                assetDescription.IsCommodity = string.Equals(assetClass.Commodity, "1", StringComparison.InvariantCultureIgnoreCase);
                assetDescription.IsAccepted = true;

                // Parse item type
                var itemDefinitionType = SteamItemDefinitionType.Item;
                if (Enum.TryParse(assetClass.Type, true, out itemDefinitionType))
                {
                    assetDescription.ItemDefinitionType = itemDefinitionType;
                }
                else
                {
                    assetDescription.ItemDefinitionType = SteamItemDefinitionType.Item;
                    assetDescription.ItemType = ((String.IsNullOrEmpty(assetDescription.ItemType) && !String.IsNullOrEmpty(assetClass.Type)) ? assetClass.Type : assetDescription.ItemType);
                }

                // Parse asset description (if any)
                if (assetClass.Descriptions != null)
                {
                    var itemDescription = ParseItemDescriptionText(assetClass.Descriptions);
                    if (!string.IsNullOrEmpty(itemDescription))
                    {
                        assetDescription.Description = itemDescription;
                    }
                }

                // Parse asset tags (if any)
                if (assetClass.Tags != null)
                {
                    assetDescription.Tags = new PersistableStringDictionary(assetDescription.Tags);
                    foreach (var tag in assetClass.Tags)
                    {
                        assetDescription.Tags[tag.Category] = tag.Name;
                    }
                }
            }

            // Parse asset workshop details
            if (request.PublishedFile != null)
            {
                var publishedFile = request.PublishedFile;
                assetDescription.WorkshopFileId = publishedFile.PublishedFileId;
                assetDescription.CreatorId = (publishedFile.Creator > 0 ? publishedFile.Creator : null);
                assetDescription.NameWorkshop = publishedFile.Title;
                assetDescription.DescriptionWorkshop = publishedFile.Description;
                assetDescription.PreviewUrl = publishedFile.PreviewUrl?.ToString();
                assetDescription.SubscriptionsCurrent = (long?)publishedFile.Subscriptions;
                assetDescription.SubscriptionsLifetime = (long?)publishedFile.LifetimeSubscriptions;
                assetDescription.FavouritedCurrent = (long?)publishedFile.Favorited;
                assetDescription.FavouritedLifetime = (long?)publishedFile.LifetimeFavorited;
                assetDescription.Views = (long?)publishedFile.Views;
                assetDescription.IsAccepted = (assetDescription.IsAccepted | (publishedFile.LifetimeSubscriptions > 0));
                assetDescription.TimeCreated = assetDescription.TimeCreated.Earliest(publishedFile.TimeCreated > DateTime.MinValue ? publishedFile.TimeCreated : null);
                assetDescription.TimeUpdated = assetDescription.TimeUpdated.Latest(publishedFile.TimeUpdated > DateTime.MinValue ? publishedFile.TimeUpdated : null);

                // Parse ban details
                if (publishedFile.Banned)
                {
                    assetDescription.IsBanned = publishedFile.Banned;
                    assetDescription.BanReason = publishedFile.BanReason;
                }

                // Parse asset workshop creator
                if (assetDescription.CreatorProfileId == null && publishedFile.Creator > 0)
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
                            assetDescription.CreatorProfile = importedProfile.Profile;
                            assetDescription.CreatorProfileId = importedProfile.Profile.Id;
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
                        var existingNonWorkshopTags = assetDescription.Tags.Where(x => !x.Key.StartsWith(Constants.SteamAssetTagWorkshop)).ToDictionary(x => x.Key, x => x.Value);
                        assetDescription.Tags = new PersistableStringDictionary(existingNonWorkshopTags);
                        foreach (var tag in interestingTags)
                        {
                            var tagTrimmed = tag.Replace(" ", string.Empty).Trim();
                            var tagKey = $"{Constants.SteamAssetTagWorkshop}.{tagTrimmed.ToLowerInvariant()}";
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

            // Parse asset workshop change notes
            if (request.PublishedFileChangeNotesPageHtml != null && assetDescription.TimeAccepted != null)
            {
                var changeNotes = request.PublishedFileChangeNotesPageHtml.Descendants("div").Where(x => x?.Attribute("class")?.Value?.Contains("workshopAnnouncement") == true);
                if (changeNotes != null)
                {
                    assetDescription.Changes = new PersistableChangeNotesDictionary(assetDescription.Changes);
                    foreach (var changeNote in changeNotes)
                    {
                        var timestamp = DateTime.UtcNow;
                        var headline = changeNote.Descendants("div").FirstOrDefault(x => x?.Attribute("class")?.Value?.Contains("headline") == true)?.Value;
                        var description = changeNote.Descendants("p").FirstOrDefault(x => x?.Attribute("id") != null)?.Value;
                        var updateDateTimeMatchGroup = Regex.Match(headline ?? String.Empty, @"Update:(.*)").Groups;
                        var updateDateTime = (updateDateTimeMatchGroup.Count > 1)
                            ? updateDateTimeMatchGroup[1].Value.Trim()
                            : null;
                        if (DateTime.TryParseExact(updateDateTime, "d MMM, yyyy @ h:mmtt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out timestamp) ||
                            DateTime.TryParseExact(updateDateTime, "d MMM @ h:mmtt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out timestamp))
                        {
                            /*
                            // Only track changes that happened after the item was accepted
                            if (timestamp > assetDescription.TimeAccepted)
                            {
                                if (String.IsNullOrEmpty(description) && assetDescription.Changes.ContainsKey(timestamp))
                                {
                                    description = assetDescription.Changes[timestamp];
                                }
                                if (!assetDescription.Changes.ContainsKey(timestamp))
                                {
                                    assetDescription.Changes[timestamp] = (description ?? string.Empty).FirstCharToUpper();
                                    app = app ?? await _db.SteamApps.AsNoTracking().FirstOrDefaultAsync(x => x.SteamId == (appId ?? 0).ToString());
                                    await _serviceBus.SendMessageAsync(new WorkshopFileUpdatedMessage()
                                    {
                                        AppId = UInt64.Parse(app.SteamId),
                                        AppName = app?.Name,
                                        AppIconUrl = app?.IconUrl,
                                        AppColour = app?.PrimaryColor,
                                        CreatorId = assetDescription.CreatorId ?? 0,
                                        CreatorName = assetDescription.CreatorProfile?.Name,
                                        CreatorAvatarUrl = assetDescription.CreatorProfile?.AvatarUrl,
                                        ItemId = assetDescription.WorkshopFileId ?? 0,
                                        ItemType = assetDescription.ItemType,
                                        ItemShortName = assetDescription.ItemShortName,
                                        ItemName = assetDescription.Name,
                                        ItemDescription = assetDescription.Description,
                                        ItemCollection = assetDescription.ItemCollection,
                                        ItemIconUrl = assetDescription.IconUrl ?? assetDescription.IconLargeUrl,
                                        ItemImageUrl = assetDescription.PreviewUrl ?? assetDescription.IconLargeUrl ?? assetDescription.IconUrl,
                                        ItemTimeAccepted = assetDescription.TimeAccepted?.UtcDateTime ?? timestamp,
                                        ChangeTimestamp = assetDescription.TimeUpdated?.UtcDateTime ?? timestamp,
                                        ChangeNote = description
                                    });
                                }
                            }
                            */
                        }
                    }
                }
            }

            // Parse asset workshop vote data
            if (request.PublishedFileVoteData != null)
            {
                var voteData = request.PublishedFileVoteData;
                assetDescription.VotesUp = voteData.VotesUp;
                assetDescription.VotesDown = voteData.VotesDown;
            }

            // Parse asset workshop previews
            if (request.PublishedFilePreviews != null)
            {
                assetDescription.Previews = new PersistableMediaDictionary();
                foreach (var preview in request.PublishedFilePreviews.OrderBy(x => x.SortOrder))
                {
                    switch (preview.PreviewType)
                    {
                        case 0: assetDescription.Previews[preview.Url] = SteamMediaType.Image; break;
                        case 1: assetDescription.Previews[preview.YouTubeVideoId] = SteamMediaType.YouTube; break;
                        case 2: assetDescription.Previews[preview.ExternalReference] = SteamMediaType.Sketchfab; break;
                        default: _logger.LogWarning($"Unsupported published file preview type {preview.PreviewType}: {preview.Url ?? preview.ExternalReference ?? preview.YouTubeVideoId}"); break;
                    }
                }
            }

            // Parse asset icon image data
            if (assetDescription.IconId == null && !string.IsNullOrEmpty(assetDescription.IconUrl) && app?.IsActive == true)
            {
                try
                {
                    var importedImage = await _commandProcessor.ProcessWithResultAsync(new ImportFileDataRequest()
                    {
                        Url = assetDescription.IconUrl,
                        UseExisting = true
                    });
                    if (importedImage?.File != null)
                    {
                        assetDescription.Icon = importedImage.File;
                        assetDescription.IconId = importedImage.File.Id;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Unable to import asset description icon. {ex.Message}");
                }
            }

            // Parse asset description and name id from the market list page (if available)
            if (!string.IsNullOrEmpty(request.MarketListingPageHtml))
            {
                var listingAssetMatchGroup = Regex.Match(request.MarketListingPageHtml, Constants.SteamMarketListingAssetJsonRegex).Groups;
                var listingAssetJson = (listingAssetMatchGroup.Count > 1)
                    ? listingAssetMatchGroup[1].Value.Trim()
                    : null;

                if (!string.IsNullOrEmpty(listingAssetJson))
                {
                    try
                    {
                        // NOTE: This is a bit hacky, but the data we need is inside a JavaScript variable within a <script> element, so we try to parse the JSON value of the variable
                        var listingAsset = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, SteamAssetClass>>>>(listingAssetJson);
                        var listingAssetClass = listingAsset?
                            .FirstOrDefault().Value?
                            .FirstOrDefault().Value?
                            .FirstOrDefault().Value;
                        var itemDescription = ParseItemDescriptionText(
                            listingAssetClass?.Descriptions?.Select(x => new AssetClassDescriptionModel()
                            {
                                Type = x.Type,
                                Value = x.Value,
                                Color = x.Color,
                            })
                        );
                        if (string.IsNullOrEmpty(assetDescription.Description) && !string.IsNullOrWhiteSpace(itemDescription))
                        {
                            assetDescription.Description = itemDescription;
                        }
                        if (listingAssetClass?.AppId > 0 && appId == null)
                        {
                            appId = listingAssetClass.AppId;
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

                if (!string.IsNullOrEmpty(itemNameId))
                {
                    assetDescription.NameId = ulong.Parse(itemNameId);
                    if (assetDescription.MarketItem != null && String.IsNullOrEmpty(assetDescription.MarketItem.SteamId))
                    {
                        assetDescription.MarketItem.SteamId = itemNameId;
                    }
                }
            }

            // Parse asset description from the store page (if available)
            if (request.StoreItemPageHtml != null)
            {
                var itemDescriptionHtml = request.StoreItemPageHtml.Descendants("div").FirstOrDefault(x => x?.Attribute("class")?.Value == Constants.SteamStoreItemDescriptionName).Value;
                if (string.IsNullOrEmpty(assetDescription.Description) && !string.IsNullOrWhiteSpace(itemDescriptionHtml))
                {
                    // Strip any HTML tags, just get the plain-text
                    assetDescription.Description = itemDescriptionHtml.ToSafeMarkup().Trim();
                }
            }

            // Parse app specific data
            // TODO: Ensure app id is always set
            switch (appId ?? Constants.RustAppId)
            {
                // CSGO
                case Constants.CSGOAppId:
                    break;

                // Rust
                case Constants.RustAppId:

                    // Parse asset crafting components from the description text (if available)
                    if (!string.IsNullOrEmpty(assetDescription.Description))
                    {
                        // Is this asset a permanent item?
                        var isPermanentDescription = @"This item will be permanently bound to your steam account";
                        if (Regex.IsMatch(assetDescription.Description.ToPlainText(), isPermanentDescription))
                        {
                            assetDescription.Description = assetDescription.Description.Replace(isPermanentDescription, string.Empty).Trim();
                            assetDescription.IsPermanent = true;
                        }

                        // Is this asset a glowing item?
                        var hasGlowDescription = @"This skin glows in the dark";
                        if (Regex.IsMatch(assetDescription.Description.ToPlainText(), hasGlowDescription))
                        {
                            assetDescription.Description = assetDescription.Description.Replace(hasGlowDescription, string.Empty).Trim();
                            assetDescription.HasGlow = true;
                        }

                        // Is this asset a crafting component?
                        // e.g. "Cloth can be combined to craft"
                        var craftingComponentMatchGroup = Regex.Match(assetDescription.Description.ToPlainText(), @"(.*) can be combined to craft").Groups;
                        var craftingComponent = (craftingComponentMatchGroup.Count > 1)
                            ? craftingComponentMatchGroup[1].Value.Trim()
                            : null;
                        if (!string.IsNullOrEmpty(craftingComponent))
                        {
                            if (string.Equals(assetDescription.Name, craftingComponent, StringComparison.InvariantCultureIgnoreCase))
                            {
                                assetDescription.IsCraftingComponent = true;
                            }
                        }

                        // Is this asset able to be broken down in to crafting components?
                        // e.g. "Breaks down into 1x Cloth"
                        var breaksDownMatchGroup = Regex.Match(assetDescription.Description.ToPlainText(), @"Breaks down into (.*)").Groups;
                        var breaksDown = (breaksDownMatchGroup.Count > 1)
                            ? breaksDownMatchGroup[1].Value.Trim()
                            : null;
                        if (!string.IsNullOrEmpty(breaksDown))
                        {
                            assetDescription.Description = assetDescription.Description.Replace(breaksDownMatchGroup[0].Value, string.Empty).Trim();
                            assetDescription.IsBreakable = true;
                            assetDescription.BreaksIntoComponents = new PersistableAssetQuantityDictionary();

                            // e.g. "1x Cloth", "1x Wood", "1x Metal"
                            var componentMatches = Regex.Matches(breaksDown, @"(\d+)\s*x\s*([^\d\.\r\n\<]*)").OfType<Match>();
                            foreach (var componentMatch in componentMatches)
                            {
                                var componentQuantity = componentMatch.Groups[1].Value;
                                var componentName = componentMatch.Groups[2].Value;
                                if (!string.IsNullOrEmpty(componentName))
                                {
                                    assetDescription.BreaksIntoComponents[componentName] = uint.Parse(componentQuantity);
                                }
                            }
                        }

                        // Is this asset a skin container and can it be sold on the market? If so, it is PROBABLY a craftable asset
                        // e.g. "Barrels contain skins for weapons and tools."
                        // e.g. "Bags contain clothes."
                        // e.g. "Boxes contain deployables,"
                        var isSkinContainer = Regex.IsMatch(assetDescription.Description.ToPlainText(), @"(.*)s contain (skins|weapons|tools|clothes|deployables)");
                        if (isSkinContainer && (assetDescription.IsMarketable || assetDescription.MarketableRestrictionDays > 0))
                        {
                            assetDescription.IsCraftable = true;
                        }
                    }

                    // Parse asset item type from the description text (if available)
                    if (!string.IsNullOrEmpty(assetDescription.Description))
                    {
                        // e.g. "This is a skin for the Large Wood Box item." 
                        var itemTypeMatchGroup = Regex.Match(assetDescription.Description.ToPlainText(), @"skin for the (.*) item\.").Groups;
                        var itemType = (itemTypeMatchGroup.Count > 1)
                            ? itemTypeMatchGroup[1].Value.Trim()
                            : null;

                        // Is it an item skin?
                        if (!string.IsNullOrEmpty(itemType))
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
                            assetDescription.ItemType = Constants.RustItemTypeSkinContainer;
                        }
                        // Is it a non-craftable container?
                        // e.g. "This special crate acquired from a twitch drop during Trust in Rust 3 will yield a random skin"
                        else if (Regex.IsMatch(assetDescription.Description.ToPlainText(), @"crate .* random skin"))
                        {
                            assetDescription.ItemType = Constants.RustItemTypeSkinContainer;
                        }
                        // Is it a miscellaneous item?
                        // e.g. "Having this item in your Steam Inventory means you'll be able to craft it in game. If you sell, trade or break this item you will no longer have this ability in game."
                        else if (Regex.IsMatch(assetDescription.Description.ToPlainText(), @"craft it in game"))
                        {
                            assetDescription.ItemType = Constants.RustItemTypeMiscellaneous;
                        }
                        // Is it an underwear item?
                        // e.g. "Having this item in your Steam Inventory means you'll be able to select this as your players default appearance. If you sell, trade or break this item you will no longer have this ability in game."
                        else if (Regex.IsMatch(assetDescription.Description.ToPlainText(), @"players default appearance"))
                        {
                            assetDescription.ItemType = Constants.RustItemTypeUnderwear;
                        }
                    }
                    else
                    {
                        // HACK: Facepunch messed up the LR300 item descriptions (they are empty), so try fill in the blanks
                        if (assetDescription.ItemShortName == Constants.RustItemShortNameLR300 || assetDescription.Tags.Any(x => x.Value == Constants.RustItemShortNameLR300 || x.Value == Constants.RustItemTypeLR300))
                        {
                            assetDescription.ItemType = Constants.RustItemTypeLR300;
                        }
                    }

                    // Parse asset item short name
                    if (!string.IsNullOrEmpty(request.AssetItemDefinition?.RustItemShortName))
                    {
                        assetDescription.ItemShortName = request.AssetItemDefinition.RustItemShortName;
                    }
                    else if (!string.IsNullOrEmpty(assetDescription.ItemType))
                    {
                        assetDescription.ItemShortName = assetDescription.ItemType.ToRustItemShortName();
                    }

                    // Parse asset item collection (if missing and is a user created item)
                    if (string.IsNullOrEmpty(assetDescription.ItemCollection) && assetDescription.CreatorId != null && !request.SkipItemCollectionCheck)
                    {
                        // Find existing item collections we fit in to (if any)
                        var existingItemCollections = await _db.SteamAssetDescriptions
                            .Where(x => x.CreatorId == assetDescription.CreatorId)
                            .Where(x => !string.IsNullOrEmpty(x.ItemCollection))
                            .Select(x => x.ItemCollection)
                            .Distinct()
                            .ToListAsync();
                        if (existingItemCollections.Any())
                        {
                            foreach (var existingItemCollection in existingItemCollections.OrderByDescending(x => x.Length))
                            {
                                var isCollectionMatch = existingItemCollection
                                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                    .All(x => assetDescription.Name.Contains(x, StringComparison.InvariantCultureIgnoreCase));
                                if (isCollectionMatch)
                                {
                                    assetDescription.ItemCollection = existingItemCollection;
                                    break;
                                }
                            }
                        }

                        // If we don't have an item collection at this point, then there are no existing collections that we fit into  :(
                        // Find other skins similar to us and see if we can start a new item collection, with black jack and hookers...
                        if (string.IsNullOrEmpty(assetDescription.ItemCollection))
                        {
                            // Remove all common item words from the collection name (e.g. "Box", "Pants", Door", etc)
                            // NOTE: Pattern match word boundarys to prevent replacing words within words.
                            //       e.g. "Stone" in "Stonecraft Hatchet" shouldn't end up like "craft Hatchet"
                            var newItemCollection = assetDescription.Name;
                            if (!string.IsNullOrEmpty(assetDescription.ItemType))
                            {
                                foreach (var word in assetDescription.ItemType.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                                {
                                    newItemCollection = Regex.Replace(newItemCollection, $@"\b{word}\b", string.Empty, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                                }
                            }
                            foreach (var word in Constants.RustItemNameCommonWords)
                            {
                                newItemCollection = Regex.Replace(newItemCollection, $@"\b{word}\b", string.Empty, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                            }

                            // Ensure all remaining words are longer than one character, otherwise strip them out.
                            // This fixes scenarios like "Satchelo" => "o", "Rainbow Doors" => "Rainbow s", etc
                            newItemCollection = Regex.Replace(newItemCollection, @"\b(\w{1,2})\b", string.Empty, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

                            // Trim any junk characters
                            newItemCollection = newItemCollection.Trim(' ', ',', '.', '-', '\'', ':').Trim();

                            // If there is anything left, we have a unique collection name, try find others with the same name
                            if (!string.IsNullOrEmpty(newItemCollection))
                            {
                                // Count the number of other assets created by the same author that also contain the remaining unique collection words.
                                // If there is more than one item, then it must be part of a set.
                                var query = _db.SteamAssetDescriptions
                                    .Where(x => x.CreatorId == assetDescription.CreatorId)
                                    .Where(x => string.IsNullOrEmpty(x.ItemCollection));
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
                    break;

                // Unturned
                case Constants.UnturnedAppId:
                    break;

            }

            // Cleanup the asset description text
            assetDescription.Description = assetDescription.Description
                ?.Trim("&nbsp;")
                ?.Trim(' ', '.', '\t', '\n', '\r');

            // Check if this is a special/twitch drop item
            if (!assetDescription.IsTwitchDrop && !assetDescription.IsSpecialDrop)
            {
                // Does this look like a publisher item twitch drop?
                if (assetDescription.WorkshopFileId == null && assetDescription.TimeAccepted == null && !assetDescription.IsMarketable && !string.IsNullOrEmpty(assetDescription.Description) &&
                         Regex.IsMatch(assetDescription.Description, @"Twitch", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
                {
                    assetDescription.IsTwitchDrop = true;
                }
            }

            // Update last checked on (unless this is a newly created asset)
            if (!assetDescription.IsTransient)
            {
                assetDescription.TimeRefreshed = DateTimeOffset.Now;
            }

            return new UpdateSteamAssetDescriptionResponse
            {
                AssetDescription = assetDescription
            };
        }

        private string ParseItemDescriptionText(IEnumerable<AssetClassDescriptionModel> descriptions)
        {
            if (descriptions?.Any() != true)
            {
                return null;
            }
            return String.Join("\n",
                descriptions
                    .Where(x => !string.IsNullOrEmpty(x.Value))
                    .Where(x =>
                        string.Equals(x.Type, Constants.SteamAssetClassDescriptionTypeHtml, StringComparison.InvariantCultureIgnoreCase) ||
                        string.Equals(x.Type, Constants.SteamAssetClassDescriptionTypeBBCode, StringComparison.InvariantCultureIgnoreCase)
                    )
                    .Select(x =>
                    {
                        x.Value = x.Value.ToSafeMarkup().Trim();
                        return x;
                    })
                    .Select(x => (x.Color != null ? $@"<span style=""color:{x.Color.SteamColourToWebHexString()}"">{x.Value}</span>" : x.Value))
                    .Select(x => (x == " " ? "&nbsp;" : x))
                    .Where(x => !String.IsNullOrEmpty(x))
                    .ToArray()
            );
        }
    }
}
