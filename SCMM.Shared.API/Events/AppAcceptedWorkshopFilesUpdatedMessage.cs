using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;

namespace SCMM.Shared.API.Events
{
    [Topic(Name = "App-Accepted-Workshop-Files-Updated-Updated")]
    [DuplicateDetection(DiscardDuplicatesSentWithinLastMinutes = 10080 /* 7 days */)]
    public class AppAcceptedWorkshopFilesUpdatedMessage : Message
    {
        public override string Id => $"{AppId}/{String.Join('+', AcceptedWorkshopFileIds)}";

        public ulong AppId { get; set; }

        public string AppName { get; set; }

        public string AppIconUrl { get; set; }

        public string AppColour { get; set; }

        public ulong[] AcceptedWorkshopFileIds { get; set; }

        public string ViewAcceptedWorkshopFilesPageUrl { get; set; }

        public DateTimeOffset? TimeUpdated { get; set; }
    }
}
