﻿using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Shared.API.Events
{
    [Topic(Name = "App-Item-Definitions-Updated")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 1440 /* 1 day */)]
    public class AppItemDefinitionsUpdatedMessage : Message
    {
        public override string Id => $"{AppId}/{ItemDefinitionsDigest}";

        public ulong AppId { get; set; }

        public string AppName { get; set; }

        public string AppIconUrl { get; set; }

        public string AppColour { get; set; }

        public string ItemDefinitionsDigest { get; set; }

        public DateTimeOffset? TimeUpdated { get; set; }
    }
}