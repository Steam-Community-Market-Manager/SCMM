using System.Collections.Generic;

namespace SCMM.Web.Shared.Domain.DTOs.Steam
{
    public class SteamAssetFilterDTO : EntityDTO
    {
        public string SteamId { get; set; }

        public string Name { get; set; }

        public IDictionary<string, string> Options { get; set; }
    }
}
