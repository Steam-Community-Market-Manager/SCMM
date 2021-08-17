using SCMM.Steam.Data.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store
{
    public class SteamProfileMarketItem : SteamItem
    {
        [Required]
        public Guid ProfileId { get; set; }

        public SteamProfile Profile { get; set; }

        public SteamProfileMarketItemFlags Flags { get; set; }
    }
}
