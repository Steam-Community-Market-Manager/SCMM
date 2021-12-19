using CommandQuery;
using Discord.Interactions;
using SCMM.Discord.Client.Attributes;
using SCMM.Discord.Client.Commands;
using SCMM.Steam.API.Queries;

namespace SCMM.Discord.Bot.Server.Modules;

[Global]
[Group("store", "Store commands")]
public class StoreModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly IQueryProcessor _queryProcessor;

    public StoreModule(IQueryProcessor queryProcessor)
    {
        _queryProcessor = queryProcessor;
    }

    [SlashCommand("next", "Show time remaining until the next store update")]
    public async Task<RuntimeResult> GetStoreNextUpdateExpectedOnAsync()
    {
        var nextUpdateTime = await _queryProcessor.ProcessAsync(new GetStoreNextUpdateTimeRequest());
        if (nextUpdateTime == null || string.IsNullOrEmpty(nextUpdateTime.TimeDescription))
        {
            return InteractionResult.Fail(
                $"I have no idea, something went wrong trying to figure it out."
            );
        }

        return InteractionResult.Success(
            $"Next store update is {nextUpdateTime.TimeDescription}."
        );
    }
}
