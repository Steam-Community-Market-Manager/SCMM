using Discord.Commands;
using SCMM.Discord.Client.Commands;

namespace SCMM.Discord.Bot.Server.Modules;

[Group("store")]
public class StoreModuleLegacyCommands : ModuleBase<ShardedCommandContext>
{
    [Command("next")]
    [Alias("update", "remaining", "time")]
    [Summary("Show time remaining until the next store update")]
    public async Task<RuntimeResult> SayStoreNextUpdateExpectedOnAsync()
    {
        return CommandResult.Fail(
            reason: $"Use `/store next` instead",
            explaination: $"Message based commands have been deprecated and replaced with Slash Commands because [Discord plans to remove support for unprivledged bots to read message content in April 2022](https://support-dev.discord.com/hc/en-us/articles/4404772028055)."
        );
    }
}
