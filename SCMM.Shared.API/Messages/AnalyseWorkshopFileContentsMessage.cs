using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Steam.API.Messages
{
    [Queue(Name = "Analyse-Workshop-File-Contents")]
    public class AnalyseWorkshopFileContentsMessage : Message
    {
        public string BlobName { get; set; }

        public bool Force { get; set; }
    }
}
