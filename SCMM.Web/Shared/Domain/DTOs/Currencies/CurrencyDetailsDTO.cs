namespace SCMM.Web.Shared.Domain.DTOs.Currencies
{
    public class CurrencyDetailsDTO : CurrencyDTO
    {
        public string SteamId { get; set; }

        public decimal ExchangeRateMultiplier { get; set; }
        
        public bool IsDefault { get; set; }
    }
}
