using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Store;
using SCMM.Steam.Data.Models.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Steam.Data.Store
{
    public class SteamApp : Entity, IApp
    {
        public SteamApp()
        {
            AssetFilters = new Collection<SteamAssetFilter>();
            AssetDescriptions = new Collection<SteamAssetDescription>();
            MarketItems = new Collection<SteamMarketItem>();
            StoreItems = new Collection<SteamStoreItem>();
            ItemStores = new Collection<SteamItemStore>();
        }

        [NotMapped]
        ulong IApp.Id => UInt64.Parse(SteamId);

        [Required]
        public string SteamId { get; set; }

        [Required]
        public string Name { get; set; }

        public string IconUrl { get; set; }

        public string IconLargeUrl { get; set; }

        public string PrimaryColor { get; set; }

        public string SecondaryColor { get; set; }

        public string TertiaryColor { get; set; }

        public string SurfaceColor { get; set; }

        public string BackgroundColor { get; set; }

        public string Subdomain { get; set; }

        public string ItemDefinitionsDigest { get; set; }

        public ulong? MostRecentlyAcceptedWorkshopFileId { get; set; }

        public DateTimeOffset? TimeUpdated { get; set; }

        public SteamAppFeatureTypes Features { get; set; }

        public ICollection<SteamAssetFilter> AssetFilters { get; set; }

        public ICollection<SteamAssetDescription> AssetDescriptions { get; set; }

        public ICollection<SteamMarketItem> MarketItems { get; set; }

        public ICollection<SteamStoreItem> StoreItems { get; set; }

        public ICollection<SteamItemStore> ItemStores { get; set; }

        public bool IsActive { get; set; }
    }
}
