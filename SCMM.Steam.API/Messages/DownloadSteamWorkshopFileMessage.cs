using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Steam.API.Messages
{
    [Queue(Name = "Steam-Workshop-File-Downloads")]
    public class DownloadSteamWorkshopFileMessage : IMessage
    {
        public ulong AppId { get; set; }

        public ulong PublishedFileId { get; set; }

        public bool Force { get; set; }
    }
}
