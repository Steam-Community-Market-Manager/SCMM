using System.Collections.Generic;

namespace SCMM.Web.Shared.Domain.DTOs.Steam
{
    public class SteamAppDTO : EntityDTO
    {
        public string SteamId { get; set; }

        public string Name { get; set; }

        public string IconUrl { get; set; }

        public ICollection<SteamItemDTO> Items { get; set; }
    }
}
