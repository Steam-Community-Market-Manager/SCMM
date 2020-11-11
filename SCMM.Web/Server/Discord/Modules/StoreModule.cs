using Discord.Commands;
using SCMM.Web.Server.Services;
using SCMM.Web.Shared;
using System;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Discord.Modules
{
    [Group("store")]
    public class StoreModule : ModuleBase<SocketCommandContext>
    {
        private readonly SteamService _steam;

        public StoreModule(SteamService steam)
        {
            _steam = steam;
        }

        /// <summary>
        /// !store remaining
        /// </summary>
        /// <returns></returns>
        [Command("remaining")]
        [Alias("next", "update", "expected")]
        [Summary("Echoes time remaining until the next expected store update")]
        public async Task SayStoreNextUpdateExpectedOnAsync()
        {
            var nextUpdateExpectedOn = _steam.GetStoreNextUpdateExpectedOn();
            if (nextUpdateExpectedOn == null)
            {
                await ReplyAsync(
                    $"I have no idea, something went wrong trying to figure it out"
                );
            }

            var remainingTime = (nextUpdateExpectedOn.Value - DateTimeOffset.Now).ToDurationString(
                showMinutes: false,
                showSeconds: false
            );
            if (!String.IsNullOrEmpty(remainingTime))
            {
                await ReplyAsync(
                    $"Next store update is expected in about **{remainingTime}** from now"
                );
            }
            else
            {
                await ReplyAsync(
                    $"Next store update is expected **any moment** now"
                );
            }
        }
    }
}
