using SCMM.Web.Shared;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Web.Server.Data.Models.Steam
{
    public class SteamCurrency : Entity, ICurrency, IExchangeableCurrency
    {
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

        /// <summary>
        /// If true, this is the system currency that all others are converted from
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// If true, this is a "cool" price which is highlighted when presenting "common" currencies
        /// </summary>
        public bool IsCommon { get; set; }
    }
}
