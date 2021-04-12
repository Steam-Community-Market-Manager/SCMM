using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store.Models.Steam
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
