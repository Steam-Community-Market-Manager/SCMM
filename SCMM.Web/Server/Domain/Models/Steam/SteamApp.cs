using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public class SteamApp : Entity
    {
        public SteamApp()
        {
            Filters = new Collection<SteamAssetFilter>();
            Assets = new Collection<SteamAssetDescription>();
            WorkshopFiles = new Collection<SteamAssetWorkshopFile>();
            StoreItems = new Collection<SteamStoreItem>();
            MarketItems = new Collection<SteamMarketItem>();
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

        public ICollection<SteamAssetDescription> Assets { get; set; }

        public ICollection<SteamAssetWorkshopFile> WorkshopFiles { get; set; }

        public ICollection<SteamStoreItem> StoreItems { get; set; }

        public ICollection<SteamMarketItem> MarketItems { get; set; }
    }
}
