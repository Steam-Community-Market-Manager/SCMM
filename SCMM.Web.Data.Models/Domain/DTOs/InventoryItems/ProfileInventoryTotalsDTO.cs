﻿using SCMM.Steam.Data.Models.Domain.Currencies;

namespace SCMM.Web.Data.Models.Domain.DTOs.InventoryItems
{
    public class ProfileInventoryTotalsDTO
    {
        public CurrencyDTO Currency { get; set; }

        public int TotalItems { get; set; }

        public long? TotalInvested { get; set; }

        public long TotalMarketValue { get; set; }

        public long TotalMarket24hrMovement { get; set; }

        public long TotalResellValue { get; set; }

        public long TotalResellTax { get; set; }

        public long TotalResellProfit { get; set; }
    }
}
