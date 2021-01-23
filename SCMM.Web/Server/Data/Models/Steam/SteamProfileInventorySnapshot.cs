using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Data.Models.Steam
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
