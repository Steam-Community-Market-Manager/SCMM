using SCMM.Shared.Data.Store;
using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store
{
    public class SteamAssetWorkshopFile : Entity
    {
        [Required]
        public Guid AppId { get; set; }

        public SteamApp App { get; set; }

        [Required]
        public ulong WorkshopFileId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string ImageUrl { get; set; }

        public Guid? ImageId { get; set; }

        public ImageData Image { get; set; }

        public Guid? CreatorId { get; set; }

        public SteamProfile Creator { get; set; }

        public DateTimeOffset TimeCreated { get; set; }

        public DateTimeOffset TimeUpdated { get; set; }

        public DateTimeOffset? TimeAccepted { get; set; }

        /// <summary>
        /// Last time this asset workshop file was updated from Steam
        /// </summary>
        public DateTimeOffset? TimeRefreshed { get; set; }

        public long Subscriptions { get; set; }

        public int Favourited { get; set; }

        public int Views { get; set; }

        public string BanReason { get; set; }
    }
}
