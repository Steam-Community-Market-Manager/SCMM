using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Topic(Name = "Currency-Exchange-Rate-Updated")]
    public class CurrencyExchangeRateUpdatedMessage : IMessage
    {
        public DateTime Timestamp { get; set; }

        public string Currency { get; set; }

        public decimal ExchangeRateMultiplier { get; set; }
    }
}
