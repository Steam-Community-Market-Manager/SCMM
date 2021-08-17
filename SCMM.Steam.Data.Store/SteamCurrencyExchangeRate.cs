using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Steam.Data.Store
{
    public class SteamCurrencyExchangeRate
    {
        [Required]
        [MaxLength(3)]
        public string CurrencyId { get; set; }

        [Required]
        public DateTimeOffset Timestamp { get; set; }

        [Column(TypeName = "decimal(29,21)")]
        public decimal ExchangeRateMultiplier { get; set; }
    }
}
