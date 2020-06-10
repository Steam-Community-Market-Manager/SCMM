using System;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public class SteamInventoryItem : SteamItem
    {
        public int Quantity { get; set; }

        public Guid? CurrencyId { get; set; }

        public SteamCurrency Currency { get; set; }

        public int BuyPrice { get; set; }
    }
}
