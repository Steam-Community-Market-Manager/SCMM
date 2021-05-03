using CommandQuery;
using Discord.Commands;
using SCMM.Steam.API.Queries;
using System;
using System.Threading.Tasks;

namespace SCMM.Discord.Bot.Server.Modules
{
    [Group("store")]
    public class StoreModule : ModuleBase<SocketCommandContext>
    {
        private readonly IQueryProcessor _queryProcessor;

        public StoreModule(IQueryProcessor queryProcessor)
        {
            _queryProcessor = queryProcessor;
        }

        [Command("next")]
        [Alias("update", "remaining", "time")]
        [Summary("Show time remaining until the next store update")]
        public async Task SayStoreNextUpdateExpectedOnAsync()
        {
            var nextUpdateTime = await _queryProcessor.ProcessAsync(new GetStoreNextUpdateTimeRequest());
            if (nextUpdateTime == null || String.IsNullOrEmpty(nextUpdateTime.TimeDescription))
            {
                await ReplyAsync(
                    $"I have no idea, something went wrong trying to figure it out."
                );
            }

            await ReplyAsync(
                $"Next store update is {nextUpdateTime.TimeDescription}."
            );
        }
    }
}
