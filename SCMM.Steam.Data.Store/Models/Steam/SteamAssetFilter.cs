using SCMM.Data.Shared.Store.Types;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store.Models.Steam
{
    public class SteamAssetFilter : Entity
    {
        public SteamAssetFilter()
        {
            Options = new PersistableStringDictionary();
        }

        [Required]
        public string SteamId { get; set; }

        [Required]
        public string Name { get; set; }

        public PersistableStringDictionary Options { get; set; }

    }
}
