using System;

namespace SCMM.Web.Shared.Domain.DTOs.Steam
{
    public class SteamMarketItemDTO : EntityDTO
    {
        public string SteamId { get; set; }

        public string Name { get; set; }

        public SteamAppDTO App { get; set; }

        public SteamAssetDescriptionDTO Description { get; set; }

        public SteamCurrencyDTO Currency { get; set; }

        public SteamMarketItemOrderDTO[] BuyOrders { get; set; }

        public SteamMarketItemOrderDTO[] SellOrders { get; set; }

        public int Supply { get; set; }

        public int Demand { get; set; }

        public int SellLowestPrice { get; set; }

        public int SellLowestDelta { get; set; }

        public int ResellPrice { get; set; }

        public int ResellTax { get; set; }

        public int ResellProfit { get; set; }

        public bool IsResellProfit { get; set; }

        public bool IsResellLoss { get; set; }

        public DateTimeOffset? LastCheckedOn { get; set; }
    }
}
