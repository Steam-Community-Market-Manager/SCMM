using SCMM.Web.Server.Data.Types;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public class SteamStoreItem : SteamItem
    {
        public SteamStoreItem()
        {
            StorePrices = new PersistablePriceDictionary();
        }

        public PersistablePriceDictionary StorePrices { get; set; }
    }
}
