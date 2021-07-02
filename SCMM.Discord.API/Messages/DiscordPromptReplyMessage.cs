using SCMM.Azure.ServiceBus;

namespace SCMM.Discord.API.Messages
{
    public class DiscordPromptReplyMessage : IMessage
    {
        public string Reply { get; set; }
    }
}
