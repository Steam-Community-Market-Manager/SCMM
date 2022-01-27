using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Web.Data.Models.UI.App
{
    public class AppDetailedDTO : AppDTO
    {
        public Guid Guid { get; set; }

        public string Subdomain { get; set; }

        public SteamStoreTypes StoreTypes { get; set; }
    }
}
