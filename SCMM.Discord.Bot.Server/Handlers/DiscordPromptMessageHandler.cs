using Discord;
using SCMM.Azure.ServiceBus;
using SCMM.Discord.API.Messages;
using SCMM.Discord.Client;
using SCMM.Discord.Client.Extensions;

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
                idOrUsername: message.Username,
                message: message.Message,
                title: message.Title,
                description: message.Description,
                fields: message.Fields,
                fieldsInline: message.FieldsInline,
                url: message.Url,
                thumbnailUrl: message.ThumbnailUrl,
                imageUrl: message.ImageUrl,
                color: message.Colour,
                reactions: message.Reactions
            );

            var waitForReplySubscription = (IDisposable)null;
            switch (message.Type)
            {
                case DiscordPromptMessage.PromptType.Reply:
                    waitForReplySubscription = _client.SubscribeToReplies(messageId,
                        (msg) => string.Equals(message.Username, msg.Author.GetFullUsername(), StringComparison.InvariantCultureIgnoreCase),
                        async (msg) =>
                        {
                            waitForReplySubscription?.Dispose();
                            await msg.AddReactionAsync(new Emoji("👌"));
                            await context.ReplyAsync(new DiscordPromptMessageReply()
                            {
                                Reply = msg.Content
                            });
                        }
                    );
                    break;

                case DiscordPromptMessage.PromptType.React:
                    waitForReplySubscription = _client.SubscribeToReactions(messageId,
                        (user, reaction) => string.Equals(message.Username, user.GetFullUsername(), StringComparison.InvariantCultureIgnoreCase),
                        async (msg, reaction) =>
                        {
                            waitForReplySubscription?.Dispose();
                            if (msg != null)
                            {
                                await msg.AddReactionAsync(new Emoji("👌"));
                            }
                            await context.ReplyAsync(new DiscordPromptMessageReply()
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
