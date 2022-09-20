using SCMM.Shared.Data.Store;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store
{
    public class SteamItemDefinitionsArchive : Entity
    {
        [Required]
        public Guid AppId { get; set; }

        public SteamApp App { get; set; }

        [Required]
        public string Digest { get; set; }

        [Required]
        public string ItemDefinitions { get; set; }

        public DateTimeOffset TimePublished { get; set; }

    }
}
