using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public class SteamApp : Entity
    {
        public SteamApp()
        {
            MarketItems = new Collection<SteamMarketItem>();
        }

        [Required]
        public string SteamId { get; set; }

        [Required]
        public string Name { get; set; }

        public string IconUrl { get; set; }

        public ICollection<SteamMarketItem> MarketItems { get; set; }
    }
}
