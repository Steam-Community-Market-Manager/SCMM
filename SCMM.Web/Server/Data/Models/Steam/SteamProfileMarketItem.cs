using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Data.Models.Steam
{
    public class SteamProfileMarketItem : SteamItem
    {
        [Required]
        public Guid ProfileId { get; set; }

        public SteamProfile Profile { get; set; }

        public SteamProfileMarketItemFlags Flags { get; set; }
    }
}
