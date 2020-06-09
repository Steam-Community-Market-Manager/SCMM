using System;

namespace SCMM.Web.Shared.Domain.DTOs.Steam
{
    public class SteamAssetDescriptionDTO : EntityDTO
    {
        public string SteamId { get; set; }

        public string Name { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public string IconLargeUrl { get; set; }

        public SteamAssetWorkshopFileDTO WorkshopFile { get; set; }

        public string[] Tags { get; set; }

        public DateTimeOffset? LastCheckedOn { get; set; }
    }
}
