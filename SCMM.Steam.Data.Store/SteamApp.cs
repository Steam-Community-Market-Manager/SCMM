using SCMM.Shared.Data.Store;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store
{
    public class SteamApp : Entity
    {
        public SteamApp()
        {
            Filters = new Collection<SteamAssetFilter>();
            AssetDescriptions = new Collection<SteamAssetDescription>();
            MarketItems = new Collection<SteamMarketItem>();
            StoreItems = new Collection<SteamStoreItem>();
            ItemStores = new Collection<SteamItemStore>();
        }

        [Required]
        public string SteamId { get; set; }

        [Required]
        public string Name { get; set; }

        public string IconUrl { get; set; }

        public string IconLargeUrl { get; set; }

        public string PrimaryColor { get; set; }

        public string SecondaryColor { get; set; }

        public string BackgroundColor { get; set; }

        public ICollection<SteamAssetFilter> Filters { get; set; }

        public ICollection<SteamAssetDescription> AssetDescriptions { get; set; }

        public ICollection<SteamMarketItem> MarketItems { get; set; }

        public ICollection<SteamStoreItem> StoreItems { get; set; }

        public ICollection<SteamItemStore> ItemStores { get; set; }

    }
}
