using System;

namespace SCMM.Web.Shared.Domain.DTOs.Steam
{
    public class SteamInventoryItemDTO : SteamItemDTO
    {
        public SteamCurrencyDTO Currency { get; set; }

        public int BuyPrice { get; set; }

        public int Quantity { get; set; }
    }
}
