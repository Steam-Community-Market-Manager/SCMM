using System;

namespace SCMM.Web.Shared.Domain.DTOs.Currencies
{
    public class CurrencyStateDTO : CurrencyDTO
    {
        public Guid Id { get; set; }

        public string SteamId { get; set; }

        public decimal ExchangeRateMultiplier { get; set; }
    }
}
