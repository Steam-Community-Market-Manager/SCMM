using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public abstract class SteamItem : Entity
    {
        public string SteamId { get; set; }

        [Required]
        public Guid AppId { get; set; }

        public SteamApp App { get; set; }

        [Required]
        public Guid DescriptionId { get; set; }

        public SteamAssetDescription Description { get; set; }
    }
}
