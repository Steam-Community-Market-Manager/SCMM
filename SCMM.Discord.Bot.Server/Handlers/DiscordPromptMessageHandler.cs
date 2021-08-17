using Discord;
using SCMM.Azure.ServiceBus;
using SCMM.Discord.API.Messages;
using SCMM.Discord.Client;
using SCMM.Discord.Client.Extensions;
using System.Drawing;

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

            var waitForReplySubscription = (IDisposable)null;
            switch (message.Type)
            {
                case DiscordPromptMessageType.Reply:
                    waitForReplySubscription = _client.SubscribeToReplies(messageId,
                        (msg) => string.Equals(message.Username, msg.Author.GetFullUsername(), StringComparison.InvariantCultureIgnoreCase),
                        async (msg) =>
                        {
                            waitForReplySubscription?.Dispose();
                            await msg.AddReactionAsync(new Emoji("👌"));
                            await context.ReplyAsync(new DiscordPromptReplyMessage()
                            {
                                Reply = msg.Content
                            });
                        }
                    );
                    break;

                case DiscordPromptMessageType.React:
                    waitForReplySubscription = _client.SubscribeToReactions(messageId,
                        (user, reaction) => string.Equals(message.Username, user.GetFullUsername(), StringComparison.InvariantCultureIgnoreCase),
                        async (msg, reaction) =>
                        {
                            waitForReplySubscription?.Dispose();
                            if (msg != null)
                            {
                                await msg.AddReactionAsync(new Emoji("👌"));
                            }
                            await context.ReplyAsync(new DiscordPromptReplyMessage()
                            {
                                Reply = reaction.Emote?.Name
                            });
                        }
                    );
                    break;
            }
        }
    }
}
