using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Steam.API.Messages
{
    [Topic(Name = "Currency-Exchange-Rates")]
    public class CurrencyExchangeRateUpdateMessage : IMessage
    {
        public DateTime Timestamp { get; set; }

        public string Currency { get; set; }

        public decimal ExchangeRateMultiplier { get; set; }
    }
}
