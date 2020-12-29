using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Data.Models.Steam
{
    public class SteamApp : Entity
    {
        public SteamApp()
        {
            Filters = new Collection<SteamAssetFilter>();
            WorkshopFiles = new Collection<SteamAssetWorkshopFile>();
            Assets = new Collection<SteamAssetDescription>();
            MarketItems = new Collection<SteamMarketItem>();
            StoreItems = new Collection<SteamStoreItem>();
            ItemStores = new Collection<SteamItemStore>();
        }

        [Required]
        public string SteamId { get; set; }

        [Required]
        public string Name { get; set; }

        public string IconUrl { get; set; }

        public Guid? IconId { get; set; }

        public ImageData Icon { get; set; }

        public string IconLargeUrl { get; set; }

        public Guid? IconLargeId { get; set; }

        public ImageData IconLarge { get; set; }

        public string PrimaryColor { get; set; }

        public string SecondaryColor { get; set; }

        public string BackgroundColor { get; set; }

        public ICollection<SteamAssetFilter> Filters { get; set; }

        public ICollection<SteamAssetWorkshopFile> WorkshopFiles { get; set; }

        public ICollection<SteamAssetDescription> Assets { get; set; }

        public ICollection<SteamMarketItem> MarketItems { get; set; }

        public ICollection<SteamStoreItem> StoreItems { get; set; }

        public ICollection<SteamItemStore> ItemStores { get; set; }

    }
}
