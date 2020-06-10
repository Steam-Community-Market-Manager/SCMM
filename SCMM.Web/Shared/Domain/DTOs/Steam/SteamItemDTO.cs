using System;

namespace SCMM.Web.Shared.Domain.DTOs.Steam
{
    public abstract class SteamItemDTO : EntityDTO
    {
        public string SteamId { get; set; }

        public SteamAppDTO App { get; set; }

        public SteamAssetDescriptionDTO Description { get; set; }
    }
}
