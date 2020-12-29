using SCMM.Web.Server.Data.Types;
using SCMM.Web.Shared.Data.Models.Steam;
using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Data.Models.Steam
{
    public class SteamAssetDescription : Entity
    {
        public SteamAssetDescription()
        {
            Tags = new PersistableStringDictionary();
        }

        [Required]
        public string SteamId { get; set; }

        [Required]
        public Guid AppId { get; set; }

        public SteamApp App { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public Guid? IconId { get; set; }

        public ImageData Icon { get; set; }

        public string IconLargeUrl { get; set; }

        public Guid? IconLargeId { get; set; }

        public ImageData IconLarge { get; set; }

        public PersistableStringDictionary Tags { get; set; }

        public SteamAssetDescriptionFlags Flags { get; set; }

        public DateTimeOffset? LastCheckedOn { get; set; }

        public Guid? WorkshopFileId { get; set; }

        public SteamAssetWorkshopFile WorkshopFile { get; set; }

        public SteamStoreItem StoreItem { get; set; }

        public SteamMarketItem MarketItem { get; set; }
    }
}
