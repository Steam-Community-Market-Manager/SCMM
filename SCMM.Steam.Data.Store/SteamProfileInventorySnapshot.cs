using System;
using System.ComponentModel.DataAnnotations;
using SCMM.Shared.Data.Store;

namespace SCMM.Steam.Data.Store
{
    public class SteamProfileInventorySnapshot : Entity
    {
        [Required]
        public Guid ProfileId { get; set; }

        public SteamProfile Profile { get; set; }

        [Required]
        public DateTimeOffset Timestamp { get; set; }

        public Guid? CurrencyId { get; set; }

        public SteamCurrency Currency { get; set; }

        public long InvestedValue { get; set; }

        public long MarketValue { get; set; }

        public int TotalItems { get; set; }
    }
}
