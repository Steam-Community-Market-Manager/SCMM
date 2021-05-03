using SCMM.Shared.Azure.ServiceBus;
using SCMM.Shared.Azure.ServiceBus.Attributes;
using System;

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
