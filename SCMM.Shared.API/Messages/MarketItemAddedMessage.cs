﻿using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Messages
{
    [Topic(Name = "Market-Item-Added")]
    public class MarketItemAddedMessage : Message
    {
        public ulong AppId { get; set; }

        public string AppName { get; set; }

        public string AppIconUrl { get; set; }

        public string AppColour { get; set; }

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
    }
}
