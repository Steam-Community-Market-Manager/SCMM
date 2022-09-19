using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Events
{
    [Topic(Name = "Store-Added")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 10080 /* 7 days */)]
    public class StoreAddedMessage : Message
    {
        public override string Id => $"{AppId}/{StoreId}";

        public ulong AppId { get; set; }

        public string AppName { get; set; }

        public string AppIconUrl { get; set; }

        public string AppColour { get; set; }

        public string StoreId { get; set; }

        public string StoreName { get; set; }

        public Item[] Items { get; set; }

        public string ItemsImageUrl { get; set; }

        public class Item
        {
            public string Name { get; set; }

            public string Currency { get; set; }

            public long? Price { get; set; }

            public string PriceDescription { get; set; }
        }
    }
}
