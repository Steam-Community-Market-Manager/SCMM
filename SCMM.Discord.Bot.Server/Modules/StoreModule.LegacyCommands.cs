using Discord.Commands;
using SCMM.Discord.Client.Commands;

namespace SCMM.Discord.Bot.Server.Modules;

[Group("store")]
public class StoreModuleLegacyCommands : ModuleBase<ShardedCommandContext>
{
    [Command("next")]
    [Alias("update", "remaining", "time")]
    [Summary("Show time remaining until the next store update")]
    public RuntimeResult SayStoreNextUpdateExpectedOnAsync()
    {
        return CommandResult.Fail(
            reason: $"Use `/store next` instead",
            explaination: $"Message based commands are being deprecated and replaced with Slash Commands because [Discord will remove support for unprivledged bots to read message content on 1st May 2022](https://dis.gd/mcfaq). If the new slash commands don't show up in your server, you'll need to kick the bot and then [reinvite it](https://discord.com/api/oauth2/authorize?client_id=761034518424715264&permissions=18496&scope=bot%20applications.commands) again with the new command permission."
        );
    }
}
