using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public class SteamCurrency : Entity
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
        /// Number of decimal places when formatted
        /// </summary>
        public int Scale { get; set; }

        /// <summary>
        /// MAX: 12345.678901234567890123456789
        /// </summary>
        [Column(TypeName = "decimal(29, 24)")]
        public decimal ExchangeRateMultiplier { get; set; }

        /// <summary>
        /// If true, this is the baseline currency that all others are converted from
        /// </summary>
        public bool IsDefault { get; set; }
    }
}
