using SCMM.Azure.ServiceBus;
using SCMM.Discord.API.Messages;
using SCMM.Discord.Client;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace SCMM.Discord.Bot.Server.Handlers
{
    public class DiscordPromptMessageHandler : IMessageHandler<DiscordPromptMessage>
    {
        private readonly DiscordClient _client;

        public DiscordPromptMessageHandler(DiscordClient client)
        {
            _client = client;
        }

        public async Task HandleAsync(DiscordPromptMessage message, MessageContext context)
        {
            var messageId = await _client.SendMessageAsync(
                username: message.Username,
                message: message.Message,
                title: message.Title,
                description: message.Description,
                fields: message.Fields,
                fieldsInline: message.FieldsInline,
                url: message.Url,
                thumbnailUrl: message.ThumbnailUrl,
                imageUrl: message.ImageUrl,
                color: ColorTranslator.FromHtml(message.Colour),
                reactions: message.Reactions
            );

            var replySubscription = (IDisposable)null;
            switch (message.Type)
            {
                case DiscordPromptMessageType.Reply:
                    replySubscription = _client.SubscribeToReplies(messageId, async x =>
                    {
                        await context.ReplyAsync(new DiscordPromptReplyMessage()
                        {
                            Reply = x
                        });
                        replySubscription.Dispose();
                    });
                    break;

                case DiscordPromptMessageType.React:
                    replySubscription = _client.SubscribeToReactions(messageId, async x =>
                    {
                        await context.ReplyAsync(new DiscordPromptReplyMessage()
                        {
                            Reply = x
                        });
                        replySubscription.Dispose();
                    });
                    break;
            }
        }
    }
}
