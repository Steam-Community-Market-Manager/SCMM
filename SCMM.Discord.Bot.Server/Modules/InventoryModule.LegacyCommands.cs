using Discord.Commands;
using SCMM.Discord.Client.Commands;

namespace SCMM.Discord.Bot.Server.Modules;

[Group("inventory")]
[Alias("inv")]
public class InventoryModuleLegacyCommands : ModuleBase<ShardedCommandContext>
{
    [Command]
    [Priority(0)]
    [Name("value")]
    [Alias("value")]
    [Summary("Show the current inventory market value for a Steam profile. This only works if the profile and the inventory privacy is set as public.")]
    public RuntimeResult SayProfileInventoryValueAsync(
        [Name("steam_id")][Summary("Valid SteamID or Steam URL")] string steamId = null,
        [Name("currency_id")][Summary("Supported three-letter currency code (e.g. USD, EUR, AUD)")] string currencyId = null
    )
    {
        return CommandResult.Fail(
            reason: $"Use `/inventory value` instead",
            explaination: $"Message based commands are being deprecated and replaced with Slash Commands because [Discord will remove support for unprivledged bots to read message content on 1st May 2022](https://dis.gd/mcfaq). If the new slash commands don't show up in your server, you'll need to kick the bot and then [reinvite it](https://discord.com/api/oauth2/authorize?client_id=761034518424715264&permissions=18496&scope=bot%20applications.commands) again with the new command permission."
        );
    }
}
