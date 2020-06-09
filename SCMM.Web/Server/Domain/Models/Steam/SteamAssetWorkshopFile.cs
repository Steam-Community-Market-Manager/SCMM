using SCMM.Web.Server.Data.Types;
using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public class SteamAssetWorkshopFile : Entity
    {
        [Required]
        public string SteamId { get; set; }
        
        public string CreatorSteamId { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset UpdatedOn { get; set; }
        
        public int Subscriptions { get; set; }

        public int Favourited { get; set; }

        public int Views { get; set; }

        public DateTimeOffset? LastCheckedOn { get; set; }
    }
}
