using System.ComponentModel.DataAnnotations;
using SCMM.Shared.Data.Store.Types;
using SCMM.Shared.Data.Store;

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

        public PersistableStringDictionary Options { get; set; }

    }
}
