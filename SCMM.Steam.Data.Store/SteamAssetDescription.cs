using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Attributes;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store.Types;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SCMM.Steam.Data.Store
{
    public class SteamAssetDescription : Entity
    {
        public SteamAssetDescription()
        {
            Notes = new PersistableStringCollection();
            Changes = new PersistableChangeNotesDictionary();
            Tags = new PersistableStringDictionary();
            Previews = new PersistableMediaDictionary();
            CraftingComponents = new PersistableAssetQuantityDictionary();
            BreaksIntoComponents = new PersistableAssetQuantityDictionary();
        }

        [Required]
        public Guid AppId { get; set; }

        public SteamApp App { get; set; }

        public ulong? ClassId { get; set; }

        /// <summary>
        /// e.g. Publisher Item, Workshop Item
        /// </summary>
        public SteamAssetDescriptionType AssetType { get; set; }

        public ulong? WorkshopFileId { get; set; }

        public string WorkshopFileUrl { get; set; }

        public ulong? CreatorId { get; set; }

        public Guid? CreatorProfileId { get; set; }

        public SteamProfile CreatorProfile { get; set; }

        public ulong? ItemDefinitionId { get; set; }

        /// <summary>
        /// e.g. Large Wood Box, Sheet Metal Door, etc
        /// </summary>
        public string ItemType { get; set; }

        /// <summary>
        /// e.g. door.hinged.metal, wall.frame.garagedoor, etc
        /// </summary>
        public string ItemShortName { get; set; }

        /// <summary>
        /// e.g. Blackout, Whiteout, etc
        /// </summary>
        public string ItemCollection { get; set; }

        public string Name { get; set; }

        public string NameHash { get; set; }

        public string NameWorkshop { get; set; }

        public ulong? NameId { get; set; }

        public string Description { get; set; }

        public string DescriptionWorkshop { get; set; }

        [Required]
        public PersistableStringCollection Notes { get; set; }

        [Required]
        public PersistableChangeNotesDictionary Changes { get; set; }

        [Required]
        public PersistableStringDictionary Tags { get; set; }

        /// <summary>
        /// If true, the item is always available for purchase. Otherwise, it is assumed to be purchasable for a limited time only.
        /// </summary>
        public bool IsPermanent { get; set; }

        public bool? HasGlow { get; set; }

        /// <summary>
        /// Only applies to guns, will be null for all other item types
        /// </summary>
        public bool? HasGlowSights { get; set; }

        [Precision(20, 20)]
        public decimal? GlowRatio { get; set; }

        public bool? HasCutout { get; set; }

        [Precision(20, 20)]
        public decimal? CutoutRatio { get; set; }

        /// <summary>
        /// The most dominant colour of the item
        /// </summary>
        public string DominantColour { get; set; }

        /// <summary>
        /// Steam item type background colour
        /// </summary>
        public string BackgroundColour { get; set; }

        /// <summary>
        /// Steam item type foreground colour
        /// </summary>
        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public Guid? IconId { get; set; }

        public FileData Icon { get; set; }

        public string IconLargeUrl { get; set; }

        public string PreviewUrl { get; set; }

        [Required]
        public PersistableMediaDictionary Previews { get; set; }

        public long? SupplyTotalEstimated { get; set; }

        public long? SupplyTotalMarketsKnown { get; set; }

        public long? SupplyTotalInvestorsKnown { get; set; }

        public long? SupplyTotalInvestorsEstimated { get; set; }

        public long? SupplyTotalOwnersKnown { get; set; }

        public long? SupplyTotalOwnersEstimated { get; set; }

        public long? SubscriptionsCurrent { get; set; }

        public long? SubscriptionsLifetime { get; set; }

        public long? FavouritedCurrent { get; set; }

        public long? FavouritedLifetime { get; set; }

        public long? Views { get; set; }

        public uint? VotesUp { get; set; }

        public uint? VotesDown { get; set; }

        public bool IsCommodity { get; set; }

        public bool IsMarketable { get; set; }

        public int? MarketableRestrictionDays { get; set; }

        public bool IsTradable { get; set; }

        public int? TradableRestrictionDays { get; set; }

        /// <summary>
        /// Frog Boots, etc
        /// </summary>
        public bool IsSpecialDrop { get; set; }

        public bool IsTwitchDrop { get; set; }

        public bool IsCraftingComponent { get; set; }

        public bool IsCraftable { get; set; }

        [Required]
        public PersistableAssetQuantityDictionary CraftingComponents { get; set; }

        public bool IsBreakable { get; set; }

        [Required]
        public PersistableAssetQuantityDictionary BreaksIntoComponents { get; set; }

        public bool IsBanned { get; set; }

        public string BanReason { get; set; }

        public bool IsAccepted { get; set; }

        public DateTimeOffset? TimeAccepted { get; set; }

        public DateTimeOffset? TimeUpdated { get; set; }

        public DateTimeOffset? TimeCreated { get; set; }

        /// <summary>
        /// Last time this asset description was updated from Steam
        /// </summary>
        public DateTimeOffset? TimeRefreshed { get; set; }

        public ICollection<SteamProfileInventoryItem> InventoryItems { get; set; }

        public ICollection<SteamStoreItemTopSellerPosition> StoreItemTopSellerPositions { get; set; }

        #region Pricing

        public SteamStoreItem StoreItem { get; set; }

        public SteamMarketItem MarketItem { get; set; }

        public MarketPrice GetCheapestBuyPrice(IExchangeableCurrency currency)
        {
            // TODO: Currently prioritises first part markets over third party markets, re-think this....
            return GetBuyPrices(currency)
                .Where(x => x.IsAvailable)
                .OrderByDescending(x => x.IsFirstPartyMarket)
                .ThenBy(x => x.Price + x.Fee)
                .FirstOrDefault();
        }

        public IEnumerable<MarketPrice> GetBuyPrices(IExchangeableCurrency currency)
        {
            // Store price
            if (StoreItem != null && StoreItem.Currency != null)
            {
                var app = (StoreItem.App ?? App);
                var appId = (app?.SteamId);
                var lowestPrice = 0L;
                if (currency != null)
                {
                    if (StoreItem.Prices != null && StoreItem.Prices.ContainsKey(currency.Name))
                    {
                        lowestPrice = StoreItem.Prices[currency.Name];
                    }
                    else
                    {
                        lowestPrice = currency.CalculateExchange(StoreItem.Price ?? 0, StoreItem.Currency);
                    }
                }
                else
                {
                    currency = StoreItem.Currency;
                    lowestPrice = StoreItem.Price ?? 0;
                }
                var buyUrl = (string)null;
                if (!String.IsNullOrEmpty(StoreItem.SteamId) && app != null)
                {
                    if (app.Features.HasFlag(SteamAppFeatureTypes.StorePersistent) || app.Features.HasFlag(SteamAppFeatureTypes.StoreRotating))
                    {
                        buyUrl = new SteamItemStoreDetailPageRequest()
                        {
                            AppId = app.SteamId,
                            ItemId = StoreItem.SteamId
                        };
                    }
                    else
                    {
                        buyUrl = new SteamBuyItemPageRequest()
                        {
                            AppId = app.SteamId,
                            ItemId = StoreItem.SteamId
                        };
                    }
                }
                else
                {
                    buyUrl = new SteamItemStorePageRequest() 
                    { 
                        AppId = appId 
                    };
                }
                yield return new MarketPrice
                {
                    Type = PriceTypes.Cash,
                    MarketType = MarketType.SteamStore,
                    Currency = currency,
                    Price = lowestPrice,
                    Fee = 0,
                    Supply = (!StoreItem.IsAvailable ? 0 : null),
                    IsAvailable = (StoreItem.IsAvailable && lowestPrice > 0),
                    Url = buyUrl
                };
            }

            // Market prices
            if (MarketItem != null && MarketItem.Currency != null)
            {
                var app = (MarketItem.App ?? App);
                foreach (var marketPrice in MarketItem.BuyPrices)
                {
                    var lowestPrice = 0L;
                    if (currency != null)
                    {
                        lowestPrice = currency.CalculateExchange(marketPrice.Value.Price, MarketItem.Currency);
                    }
                    else
                    {
                        currency = MarketItem.Currency;
                        lowestPrice = marketPrice.Value.Price;
                    }
                    yield return new MarketPrice
                    {
                        Type = (marketPrice.Key.GetMarketPriceType() ?? PriceTypes.None),
                        MarketType = marketPrice.Key,
                        Currency = currency,
                        Price = lowestPrice,
                        Fee = marketPrice.Key.GetMarketBuyFees(lowestPrice),
                        Supply = marketPrice.Value.Supply,
                        IsAvailable = (!String.IsNullOrEmpty(NameHash) && lowestPrice > 0 && marketPrice.Value.Supply != 0),
                        Url = marketPrice.Key.GetMarketBuyUrl(
                            app?.SteamId, app?.Name?.ToLower(), ClassId, NameHash
                        )
                    };
                }
            }
        }

        public IEnumerable<ItemInteraction> GetInteractions()
        {
            if (WorkshopFileId > 0)
            {
                yield return new ItemInteraction
                {
                    Icon = "fa-tools",
                    Name = "View Workshop",
                    Url = new SteamWorkshopFileDetailsPageRequest()
                    {
                        Id = WorkshopFileId.Value.ToString()
                    }
                };
            }

            if (MarketItem != null && !String.IsNullOrEmpty(NameHash) && !String.IsNullOrEmpty(MarketItem.SteamId))
            {
                yield return new ItemInteraction
                {
                    Icon = "fa-balance-scale-left",
                    Name = "View Market",
                    Url = new SteamMarketListingPageRequest()
                    {
                        AppId = (MarketItem.App?.SteamId ?? App?.SteamId),
                        MarketHashName = NameHash
                    }
                };
            }

            if (StoreItem != null)
            {
                var app = (StoreItem.App ?? App);
                var storeActionName = (string)null;
                var storeUrl = (string)null;
                if (!String.IsNullOrEmpty(StoreItem.SteamId) && StoreItem.IsAvailable && app != null)
                {
                    if (app.Features.HasFlag(SteamAppFeatureTypes.StorePersistent) || app.Features.HasFlag(SteamAppFeatureTypes.StoreRotating))
                    {
                        storeActionName = "View Store";
                        storeUrl = new SteamItemStoreDetailPageRequest()
                        {
                            AppId = app.SteamId,
                            ItemId = StoreItem.SteamId
                        };
                    }
                    else
                    {
                        storeActionName = "Buy Now";
                        storeUrl = new SteamBuyItemPageRequest()
                        {
                            AppId = app.SteamId,
                            ItemId = StoreItem.SteamId
                        };
                    }
                }
                else if (StoreItem.Stores?.Any(x => x.Store != null) == true)
                {
                    storeActionName = "View Store";
                    storeUrl = $"/store/{StoreItem.Stores.Where(x => x.Store != null).MaxBy(x => x.Store.Start).Store.StoreId()}";
                }
                if (!String.IsNullOrEmpty(storeUrl))
                {
                    yield return new ItemInteraction
                    {
                        Icon = "fa-shopping-cart",
                        Name = storeActionName,
                        Url = storeUrl
                    };
                }
            }
        }

        #endregion
    }
}
