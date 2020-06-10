using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public class SteamApp : Entity
    {
        public SteamApp()
        {
            StoreItems = new Collection<SteamStoreItem>();
            MarketItems = new Collection<SteamMarketItem>();
        }

        [Required]
        public string SteamId { get; set; }

        [Required]
        public string Name { get; set; }

        public string IconUrl { get; set; }

        public string IconLargeUrl { get; set; }

        public ICollection<SteamStoreItem> StoreItems { get; set; }

        public ICollection<SteamMarketItem> MarketItems { get; set; }
    }
}
