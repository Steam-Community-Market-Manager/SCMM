using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Events
{
    [Topic(Name = "Store-Item-Added")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 10080 /* 7 days */)]
    public class StoreItemAddedMessage : Message
    {
        public override string Id => $"{AppId}/{StoreId}/{ItemId}";

        public ulong AppId { get; set; }

        public string AppName { get; set; }

        public string AppIconUrl { get; set; }

        public string AppColour { get; set; }

        public string StoreId { get; set; }

        public string StoreName { get; set; }

        public ulong? CreatorId { get; set; }

        public string CreatorName { get; set; }

        public string CreatorAvatarUrl { get; set; }

        public ulong ItemId { get; set; }

        public string ItemType { get; set; }

        public string ItemShortName { get; set; }

        public string ItemName { get; set; }

        public string ItemDescription { get; set; }

        public string ItemCollection { get; set; }

        public string ItemImageUrl { get; set; }

        public Price[] ItemPrices { get; set; }

        public class Price
        {
            public string Currency { get; set; }

            public long Value { get; set; }

            public string Description { get; set; }
        }
    }
}
