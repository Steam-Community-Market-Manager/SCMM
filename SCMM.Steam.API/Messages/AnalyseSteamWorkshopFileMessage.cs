using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Steam.API.Messages
{
    [Topic(Name = "Steam-Workshop-File-Analyse")]
    public class AnalyseSteamWorkshopFileMessage : IMessage
    {
        public string BlobName { get; set; }
    }
}
