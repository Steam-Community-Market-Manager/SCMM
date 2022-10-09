using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;

namespace SCMM.Shared.API.Events
{
    [Topic(Name = "Store-Added")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 10080 /* 7 days */)]
    public class StoreAddedMessage : Message
    {
        public override string Id => $"{AppId}/{StoreId}+{String.Join('+', (Items ?? new Item[0]).Select(x => x.Id))}";

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
            public ulong Id { get; set; }

            public string Name { get; set; }

            public string Currency { get; set; }

            public long? Price { get; set; }

            public string PriceDescription { get; set; }
        }
    }
}
