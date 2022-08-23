using SCMM.Azure.ServiceBus;

namespace SCMM.Discord.API.Messages
{
    public class DiscordPromptMessageReply : IMessage
    {
        public string Reply { get; set; }
    }
}
