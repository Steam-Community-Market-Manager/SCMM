using System.ComponentModel.DataAnnotations;
using SCMM.Shared.Data.Store;

namespace SCMM.Steam.Data.Store
{
    public class SteamLanguage : Entity
    {
        [Required]
        public string SteamId { get; set; }

        [Required]
        public string Name { get; set; }

        public string CultureName { get; set; }

        public bool IsDefault { get; set; }
    }
}
