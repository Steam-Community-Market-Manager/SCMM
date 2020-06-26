namespace SCMM.Web.Shared.Domain.DTOs.Currencies
{
    public class CurrencyDetailsDTO
    {
        public string SteamId { get; set; }

        public string Name { get; set; }

        public string PrefixText { get; set; }

        public string SuffixText { get; set; }

        public int Scale { get; set; }

        public decimal ExchangeRateMultiplier { get; set; }
        
        public bool IsDefault { get; set; }
    }
}
