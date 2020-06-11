using SCMM.Web.Server.Data.Types;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Domain.Models.Steam
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
