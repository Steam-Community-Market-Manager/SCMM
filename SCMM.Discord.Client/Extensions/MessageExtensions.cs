using Discord;
using System.Threading.Tasks;

namespace SCMM.Discord.Client.Extensions
{
    public static class MessageExtensions
    {
        public static async Task LoadingAsync(this IUserMessage message, string text)
        {
            await message.ModifyAsync(x =>
            {
                x.Content = text;
            });
        }
    }
}
