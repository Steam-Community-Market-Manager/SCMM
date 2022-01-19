using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Store;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Steam.Data.Store
{
    public class SteamCurrency : Entity, ICurrency, IExchangeableCurrency
    {
        [NotMapped]
        uint ICurrency.Id => UInt32.Parse(SteamId);

        [Required]
        public string SteamId { get; set; }

        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Text to appear at start of price when formatted
        /// </summary>
        public string PrefixText { get; set; }

        /// <summary>
        /// Text to appear at end of price when formatted
        /// </summary>
        public string SuffixText { get; set; }

        /// <summary>
        /// Culture used to format prices
        /// </summary>
        public string CultureName { get; set; }

        /// <summary>
        /// Number of decimal places when formatted
        /// </summary>
        public int Scale { get; set; }

        /// <summary>
        /// Used to convert system currency to local currency
        /// </summary>
        [Column(TypeName = "decimal(29,21)")]
        public decimal ExchangeRateMultiplier { get; set; }
    }
}
