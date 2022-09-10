using Discord;

namespace SCMM.Discord.Client.Extensions
{
    public static class MessageExtensions
    {
        public static async Task TryAddReactionAsync(this IUserMessage message, IEmote emote)
        {
            try
            {
                // TODO: Check if "Add Reaction" permission is available first
                await message.AddReactionAsync(emote);
            }
            catch (Exception)
            {
                // Ignore...
            }
        }
    }
}
