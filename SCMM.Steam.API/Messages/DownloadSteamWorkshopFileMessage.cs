using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Steam.API.Messages
{
    [Topic(Name = "Steam-Workshop-File-Downloads")]
    public class DownloadSteamWorkshopFileMessage : IMessage
    {
        public ulong PublishedFileId { get; set; }
    }
}
