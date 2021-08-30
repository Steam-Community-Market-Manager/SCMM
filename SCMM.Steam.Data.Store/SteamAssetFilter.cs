using SCMM.Shared.Data.Store;
using SCMM.Shared.Data.Store.Types;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store
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

        [Required]
        public PersistableStringDictionary Options { get; set; }

    }
}
