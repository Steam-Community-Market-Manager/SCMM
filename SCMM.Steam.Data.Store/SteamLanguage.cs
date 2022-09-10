using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Store;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Steam.Data.Store
{
    public class SteamLanguage : Entity, ILanguage
    {
        [NotMapped]
        string ILanguage.Id => SteamId;

        [Required]
        public string SteamId { get; set; }

        [Required]
        public string Name { get; set; }

        public string CultureName { get; set; }
    }
}
