using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Store;
using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store.Types;
using System.ComponentModel.DataAnnotations;

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

        [Required]
        public ulong ClassId { get; set; }

        /// <summary>
        /// e.g. Publisher Item, Workshop Item
        /// </summary>
        public SteamAssetDescriptionType AssetType { get; set; }

        public ulong? WorkshopFileId { get; set; }

        public string WorkshopFileUrl { get; set; }

        public ulong? CreatorId { get; set; }

        public Guid? CreatorProfileId { get; set; }

        public SteamProfile CreatorProfile { get; set; }

        /// <summary>
        /// e.g. Large Wood Box, Sheet Metal Door, etc
        /// </summary>
        public string ItemType { get; set; }

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

        public Guid? IconLargeId { get; set; }

        public FileData IconLarge { get; set; }

        public string PreviewUrl { get; set; }

        public Guid? PreviewId { get; set; }

        public FileData Preview { get; set; }

        [Required]
        public PersistableMediaDictionary Previews { get; set; }

        public long? CurrentSubscriptions { get; set; }

        public long? LifetimeSubscriptions { get; set; }

        public long? CurrentFavourited { get; set; }

        public long? LifetimeFavourited { get; set; }

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

        #region Pricing

        public SteamStoreItem StoreItem { get; set; }

        public SteamMarketItem MarketItem { get; set; }

        public Price this[IExchangeableCurrency currency] =>
            GetPrices(currency).Where(x => x.IsAvailable).OrderBy(x => x.LowestPrice).FirstOrDefault();

        public IEnumerable<Price> GetPrices(IExchangeableCurrency currency)
        {
            // Steam store
            if (StoreItem != null && StoreItem.Currency != null)
            {
                var appId = (StoreItem.App?.SteamId ?? App?.SteamId);
                var lowestPrice = (long?)null;
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
                    lowestPrice = StoreItem.Price;
                }
                yield return new Price
                {
                    Type = PriceType.SteamStore,
                    Currency = currency,
                    LowestPrice = lowestPrice ?? 0,
                    QuantityAvailable = (!StoreItem.IsAvailable ? 0 : null),
                    IsAvailable = (StoreItem.IsAvailable && lowestPrice > 0),
                    Url = !string.IsNullOrEmpty(StoreItem.SteamId)
                        ? new SteamStoreItemPageRequest() { AppId = appId, ItemId = StoreItem.SteamId }
                        : new SteamStorePageRequest() { AppId = appId }
                };
            }

            // Steam community market
            if (MarketItem != null && MarketItem.Currency != null)
            {
                var appId = (MarketItem.App?.SteamId ?? App?.SteamId);
                var lowestPrice = (long?)null;
                if (currency != null)
                {
                    lowestPrice = currency.CalculateExchange(MarketItem.SellOrderLowestPrice, MarketItem.Currency);
                }
                else
                {
                    currency = MarketItem.Currency;
                    lowestPrice = MarketItem.SellOrderLowestPrice;
                }
                yield return new Price
                {
                    Type = PriceType.SteamCommunityMarket,
                    Currency = currency,
                    LowestPrice = lowestPrice ?? 0,
                    QuantityAvailable = MarketItem.SellOrderCount,
                    IsAvailable = (!String.IsNullOrEmpty(NameHash) && lowestPrice > 0 && MarketItem.SellOrderCount > 0),
                    Url = new SteamMarketListingPageRequest()
                    {
                        AppId = appId,
                        MarketHashName = NameHash
                    }
                };
            }

            if (MarketItem != null && MarketItem.SellOrderLowestPrice > 0 && MarketItem.Currency != null)
            {
                // TODO: Implement these properly...
                yield return new Price
                {
                    Type = PriceType.Skinport
                };
                yield return new Price
                {
                    Type = PriceType.BitSkins
                };
                yield return new Price
                {
                    Type = PriceType.SwapGG
                };
                yield return new Price
                {
                    Type = PriceType.TradeitGG
                };
                yield return new Price
                {
                    Type = PriceType.Dmarket
                };
                yield return new Price
                {
                    Type = PriceType.CSDeals
                };
            }
        }

        #endregion
    }
}
