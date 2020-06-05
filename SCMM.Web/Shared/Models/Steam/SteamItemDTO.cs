using System;
using System.Collections.Generic;

namespace SCMM.Web.Shared.Models.Steam
{
    public class SteamItemDTO : EntityDTO
    {
        public string SteamId { get; set; }

        public string Name { get; set; }

        public SteamItemDescriptionDTO Description { get; set; }

        public DateTimeOffset LastChecked { get; set; }
    }
}
