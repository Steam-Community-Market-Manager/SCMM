using Discord;
using SCMM.Discord.API.Messages;
using SCMM.Discord.Client;
using SCMM.Discord.Client.Extensions;
using SCMM.Shared.Abstractions.Messaging;

namespace SCMM.Discord.Bot.Server.Handlers
{
    public class DiscordPromptMessageHandler : IMessageHandler<PromptDiscordMessage>
    {
        private readonly DiscordClient _client;

        public DiscordPromptMessageHandler(DiscordClient client)
        {
            _client = client;
        }

        public async Task HandleAsync(PromptDiscordMessage message, IMessageContext context)
        {
            var messageId = await _client.SendMessageAsync(
                userIdOrName: message.Username,
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
                case PromptDiscordMessage.PromptType.Reply:
                    waitForReplySubscription = _client.SubscribeToReplies(messageId,
                        (msg) => string.Equals(message.Username, msg.Author.GetFullUsername(), StringComparison.InvariantCultureIgnoreCase),
                        async (msg) =>
                        {
                            waitForReplySubscription?.Dispose();
                            await msg.AddReactionAsync(new Emoji("👌"));
                            await context.ReplyAsync(new PromptDiscordMessage.Reply()
                            {
                                Content = msg.Content
                            });
                        }
                    );
                    break;

                case PromptDiscordMessage.PromptType.React:
                    waitForReplySubscription = _client.SubscribeToReactions(messageId,
                        (user, reaction) => string.Equals(message.Username, user.GetFullUsername(), StringComparison.InvariantCultureIgnoreCase),
                        async (msg, reaction) =>
                        {
                            waitForReplySubscription?.Dispose();
                            if (msg != null)
                            {
                                await msg.AddReactionAsync(new Emoji("👌"));
                            }
                            await context.ReplyAsync(new PromptDiscordMessage.Reply()
                            {
                                Content = reaction.Emote?.Name
                            });
                        }
                    );
                    break;
            }
        }
    }
}
