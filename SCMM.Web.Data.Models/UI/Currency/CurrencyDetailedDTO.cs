using SCMM.Shared.Data.Models;

namespace SCMM.Web.Data.Models.UI.Currency
{
    public class CurrencyDetailedDTO : CurrencyDTO, IExchangeableCurrency
    {
        public Guid Guid { get; set; }

        public uint Id { get; set; }

        public string Name { get; set; }

        public decimal ExchangeRateMultiplier { get; set; }
    }
}
