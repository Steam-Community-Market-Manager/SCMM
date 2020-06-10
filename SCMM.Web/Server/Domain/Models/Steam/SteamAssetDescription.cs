using SCMM.Web.Server.Data.Types;
using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public class SteamAssetDescription : Entity
    {
        public SteamAssetDescription()
        {
            Tags = new PersistableStringCollection();
        }

        [Required]
        public string SteamId { get; set; }
        
        [Required]
        public string Name { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public string IconLargeUrl { get; set; }

        public Guid? WorkshopFileId { get; set; }

        public SteamAssetWorkshopFile WorkshopFile { get; set; }

        public PersistableStringCollection Tags { get; set; }

        public DateTimeOffset? LastCheckedOn { get; set; }
    }
}
