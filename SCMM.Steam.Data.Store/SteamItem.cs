﻿using SCMM.Shared.Data.Store;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store
{
    public abstract class SteamItem : Entity
    {
        public string SteamId { get; set; }

        [Required]
        public Guid AppId { get; set; }

        public SteamApp App { get; set; }

        public Guid? DescriptionId { get; set; }

        public SteamAssetDescription Description { get; set; }
    }
}
