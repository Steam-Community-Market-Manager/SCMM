using CommandQuery;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SCMM.Discord.Client;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Blob;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using Steam.Models;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SCMM.Discord.Bot.Server.Modules
{
    [Group("administration")]
    [Alias("admin")]
    [RequireOwner]
    [RequireContext(ContextType.DM)]
    public class AdministrationModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _configuration;
        private readonly SteamDbContext _db;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        private readonly SteamWebClient _steamWebClient;

        public AdministrationModule(IConfiguration configuration, SteamDbContext db, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor, SteamWebClient steamWebClient)
        {
            _configuration = configuration;
            _db = db;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
            _steamWebClient = steamWebClient;
        }

        [Command("import asset")]
        public async Task<RuntimeResult> ImportSteamAssetAsync(ulong assetClassId)
        {
            const ulong rustAppId = 252490;
            const string english = "english";

            //
            // Asset class details
            //

            var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_configuration.GetSteamConfiguration().ApplicationKey);
            var steamEconomy = steamWebInterfaceFactory.CreateSteamWebInterface<SteamEconomy>();
            var assetClassInfo = await steamEconomy.GetAssetClassInfoAsync(
                (uint) rustAppId, new List<ulong>() { assetClassId }, english
            );
            if (assetClassInfo?.Data?.Success != true)
            {
                return CommandResult.Fail("Request GetAssetClassInfoAsync failed");
            }

            var assetDescription = assetClassInfo?.Data?.AssetClasses?.FirstOrDefault(x => x.ClassId == assetClassId);
            if (assetDescription == null)
            {
                return CommandResult.Fail("Asset not found");
            }

            var workshopFileId = (ulong?) null;
            var viewWorkshopAction = assetDescription?.Actions?.FirstOrDefault(x => x.Name == Constants.SteamActionViewWorkshopItem);
            if (viewWorkshopAction != null)
            {
                var workshopFileIdGroups = Regex.Match(viewWorkshopAction.Link, Constants.SteamActionViewWorkshopItemRegex).Groups;
                workshopFileId = (workshopFileIdGroups.Count > 1) ? UInt64.Parse(workshopFileIdGroups[1].Value) : 0;
            }

            //
            // Published file details
            //

            var publishedFile = (PublishedFileDetailsModel) null;
            if (workshopFileId != null && workshopFileId > 0)
            {
                var steamRemoteStorage = steamWebInterfaceFactory.CreateSteamWebInterface<SteamRemoteStorage>();
                var publishedFileDetails = await steamRemoteStorage.GetPublishedFileDetailsAsync(workshopFileId.Value);
                if (publishedFileDetails?.Data == null)
                {
                    return CommandResult.Fail("Request GetPublishedFileDetailsAsync failed");
                }

                publishedFile = publishedFileDetails.Data;
            }

            //
            // Community market details
            //
            var marketListingPageHtml = await _steamWebClient.GetText(new SteamMarketListingPageRequest()
            {
                AppId = rustAppId.ToString(),
                MarketHashName = assetDescription.MarketHashName,
            });

            //
            // Steam store details
            //
            /*
            var storePage = await _steamWebClient.GetHtml(new SteamMarketListingPageRequest()
            {
                AppId = rustAppId.ToString(),
                MarketHashName = assetDescription.MarketHashName,
            });
            */

            //
            // Calculated details
            //

            var result = new SteamAssetDescription()
            {
                AppId = rustAppId,
                AssetId = assetDescription.ClassId,
                WorkshopFileId = workshopFileId,
                CreatorId = publishedFile?.Creator,
                Type = String.IsNullOrEmpty(assetDescription.Type) ? "Publisher Item" : assetDescription.Type,
                Name = assetDescription.MarketName,
                NameHash = assetDescription.MarketHashName,
                Tags = new Dictionary<string, string>(),
                IconUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrl),
                IconLargeUrl = new SteamEconomyImageBlobRequest(assetDescription.IconUrlLarge ?? assetDescription.IconUrl),
                ImageUrl = publishedFile?.PreviewUrl,
                BackgroundColour = assetDescription.BackgroundColor?.SteamColourToHexString(),
                ForegroundColour = assetDescription.NameColor?.SteamColourToHexString(),
                CurrentSubscriptions = publishedFile?.Subscriptions,
                TotalSubscriptions = publishedFile?.LifetimeSubscriptions,
                IsCommodity = String.Equals(assetDescription.Commodity, "1", StringComparison.InvariantCultureIgnoreCase),
                IsMarketable = String.Equals(assetDescription.Marketable, "1", StringComparison.InvariantCultureIgnoreCase),
                MarketableRestrictionDays = String.IsNullOrEmpty(assetDescription.MarketMarketableRestriction) ? (uint?)null : UInt32.Parse(assetDescription.MarketMarketableRestriction),
                IsTradable = String.Equals(assetDescription.Tradable, "1", StringComparison.InvariantCultureIgnoreCase),
                TradableRestrictionDays = String.IsNullOrEmpty(assetDescription.MarketTradableRestriction) ? (uint?)null : UInt32.Parse(assetDescription.MarketTradableRestriction),
                IsBanned = publishedFile?.Banned ?? false,
                BanReason = publishedFile?.BanReason,
                TimeCreated = publishedFile?.TimeCreated,
                TimeUpdated = publishedFile?.TimeUpdated
            };

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
                            result.Description = Regex.Replace(listingAssetDescriptionHtml, "<[^>]*>", String.Empty).Trim();
                        }
                    }
                    catch(Exception)
                    {
                        // Likely because "no listings for item"
                    }
                }
                var itemNameIdMatchGroup = Regex.Match(marketListingPageHtml, "ItemActivityTicker.Start\\((.*)\\);\r\n").Groups;
                var itemNameId = (itemNameIdMatchGroup.Count > 1)
                    ? itemNameIdMatchGroup[1].Value.Trim()
                    : null;

                if (!String.IsNullOrEmpty(itemNameId))
                {
                    result.NameId = UInt64.Parse(itemNameId);
                }
            }

            if (!String.IsNullOrEmpty(result.Description))
            {
                var breakdownMatchGroup = Regex.Match(result.Description, "Breaks down into (.*)").Groups;
                var breakdown = (breakdownMatchGroup.Count > 1)
                    ? breakdownMatchGroup[1].Value.Trim()
                    : null;

                if (!String.IsNullOrEmpty(breakdown))
                {
                    result.Description = result.Description.Replace(breakdownMatchGroup[0].Value, String.Empty).Trim();
                    result.IsBreakable = true;
                    result.BreaksDownInto = new Dictionary<string, uint>();

                    var componentMatches = Regex.Matches(breakdown, "(\\d+)\\s*x\\s*(\\D*)").OfType<Match>();
                    foreach (var componentMatch in componentMatches)
                    {
                        var componentQuantity = componentMatch.Groups[1].Value;
                        var componentName = componentMatch.Groups[2].Value;
                        if (!String.IsNullOrEmpty(componentName))
                        {
                            result.BreaksDownInto[componentName] = UInt32.Parse(componentQuantity);
                        }
                    }
                }
            }

            foreach (var tag in assetDescription?.Tags)
            {
                result.Tags.Add(tag.CategoryName, tag.Name);
            }

            if (!String.IsNullOrEmpty(result.Description))
            {
                var skinMatchGroup = Regex.Match(result.Description, "skin for the (.*) item\\.").Groups;
                var skin = (skinMatchGroup.Count > 1)
                    ? skinMatchGroup[1].Value.Trim()
                    : null;
                if (!String.IsNullOrEmpty(skin))
                {
                    result.Tags["Skin"] = skin;
                }
            }

            if (result.Tags.Any())
            {
                var itemCollection = assetDescription.Name;
                foreach (var tag in result.Tags)
                {
                    itemCollection = itemCollection.Replace(tag.Value, String.Empty);
                }
                if (!String.IsNullOrEmpty(itemCollection))
                {
                    result.Tags["Collection"] = itemCollection.Trim();
                }
            }
            /*
            if (assetDescription != null)
            {
                await Context.Channel.SendMessageAsync(
                    $"Asset Description: ```json\n{JsonConvert.SerializeObject(assetDescription, Formatting.Indented)}\n```\n"
                );
            }
            if (publishedFile != null)
            {
                await Context.Channel.SendMessageAsync(
                    $"Published File: ```json\n{JsonConvert.SerializeObject(publishedFile, Formatting.Indented)}\n```"
                );
            }
            */
            return CommandResult.Success(
                $"```json\n{JsonConvert.SerializeObject(result, Formatting.Indented)}\n```"
            );
        }
    }

    public class SteamAssetDescription
    {
        public ulong AppId { get; set; }

        public ulong AssetId { get; set; }

        public ulong? WorkshopFileId { get; set; }

        public ulong? CreatorId { get; set; }

        /// <summary>
        /// "Workshop Item"
        /// </summary>
        public string Type { get; set; }

        public string Name { get; set; }

        public string NameHash { get; set; }

        public ulong? NameId { get; set; }

        public string Description { get; set; }

        public IDictionary<string, string> Tags { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public Uri IconUrl { get; set; }

        public Uri IconLargeUrl { get; set; }

        public Uri ImageUrl { get; set; }

        public ulong? CurrentSubscriptions { get; set; }

        public ulong? TotalSubscriptions { get; set; }

        public bool IsCommodity { get; set; }

        public bool IsMarketable { get; set; }

        public uint? MarketableRestrictionDays { get; set; }

        public bool IsTradable { get; set; }

        public uint? TradableRestrictionDays { get; set; }

        // TODO: Find this...
        public bool IsCraftable { get; set; }

        // TODO: Find this...
        public IDictionary<string, uint> CraftingRequirements { get; set; }

        public bool IsBreakable { get; set; }

        public IDictionary<string, uint> BreaksDownInto { get; set; }

        public bool IsBanned { get; set; }

        public string BanReason { get; set; }

        public DateTime? TimeCreated { get; set; }

        public DateTime? TimeUpdated { get; set; }

        // TODO: Find this...
        public DateTime? TimeAccepted { get; set; }
    }
}
