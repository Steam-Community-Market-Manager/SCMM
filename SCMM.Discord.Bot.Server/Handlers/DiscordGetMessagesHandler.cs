using Discord;
using SCMM.Azure.ServiceBus;
using SCMM.Discord.API.Messages;
using SCMM.Discord.Client;

namespace SCMM.Discord.Bot.Server.Handlers
{
    public class DiscordGetMessagesHandler : IMessageHandler<DiscordGetMessages>
    {
        private readonly DiscordClient _client;

        public DiscordGetMessagesHandler(DiscordClient client)
        {
            _client = client;
        }

        public async Task HandleAsync(DiscordGetMessages message, MessageContext context)
        {
            var messages = await _client.ListMessagesAsync(
                guildId: message.GuildId,
                channelId: message.ChannelId,
                messageLimit: message.MessageLimit
            );

            await context.ReplyAsync(new DiscordGetMessagesReply()
            {
                Messages = messages?.Select(m => new DiscordGetMessagesReply.Message()
                { 
                    Id = m.Id,
                    AuthorId = m.AuthorId,
                    Content = m.Content,
                    Attachments = m.Attachments?.Select(a => new DiscordGetMessagesReply.MessageAttachment()
                    {
                        Id = a.Id,
                        Url = a.Url,
                        FileName = a.FileName,
                        ContentType = a.ContentType,
                        Description = a.Description
                    })?.ToArray(),
                    Timestamp = m.Timestamp 
                })?.ToArray()
            });
        }
    }
}
