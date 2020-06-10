using System;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public class SteamStoreItem : SteamItem
    {
        public Guid? CurrencyId { get; set; }

        public SteamCurrency Currency { get; set; }

        public int StorePrice { get; set; }

        public DateTimeOffset? FirstReleasedOn { get; set; }
    }
}
