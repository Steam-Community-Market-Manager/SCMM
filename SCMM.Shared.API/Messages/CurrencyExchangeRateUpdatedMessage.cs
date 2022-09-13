using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Topic(Name = "Currency-Exchange-Rate-Updated")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 60)]
    public class CurrencyExchangeRateUpdatedMessage : Message
    {
        public override string Id => $"{Currency}/{Timestamp.Ticks}";

        public DateTime Timestamp { get; set; }

        public string Currency { get; set; }

        public decimal ExchangeRateMultiplier { get; set; }
    }
}
