using CommandQuery;
using Discord.Commands;
using SCMM.Discord.Client;
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
        public async Task<RuntimeResult> SayStoreNextUpdateExpectedOnAsync()
        {
            var nextUpdateTime = await _queryProcessor.ProcessAsync(new GetStoreNextUpdateTimeRequest());
            if (nextUpdateTime == null || String.IsNullOrEmpty(nextUpdateTime.TimeDescription))
            {
                return CommandResult.Fail(
                    $"I have no idea, something went wrong trying to figure it out."
                );
            }

            return CommandResult.Success(
                $"Next store update is {nextUpdateTime.TimeDescription}."
            );
        }
    }
}
