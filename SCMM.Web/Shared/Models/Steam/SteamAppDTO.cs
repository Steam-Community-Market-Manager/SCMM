using System;
using System.Collections.Generic;

namespace SCMM.Web.Shared.Models.Steam
{
    public class SteamAppDTO : EntityDTO
    {
        public string SteamId { get; set; }

        public string Name { get; set; }

        public string IconUrl { get; set; }

        public ICollection<SteamItemDTO> Items { get; set; }
    }
}
