using System;

namespace SCMM.Web.Shared.Domain.DTOs.Steam
{
    public class SteamItemDTO : EntityDTO
    {
        public string SteamId { get; set; }

        public string Name { get; set; }

        public SteamItemDescriptionDTO Description { get; set; }

        public DateTimeOffset LastChecked { get; set; }
    }
}
