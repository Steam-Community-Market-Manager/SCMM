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
    public async Task<RuntimeResult> SayProfileInventoryValueAsync(
        [Name("steam_id")][Summary("Valid SteamID or Steam URL")] string steamId = null,
        [Name("currency_id")][Summary("Supported three-letter currency code (e.g. USD, EUR, AUD)")] string currencyId = null
    )
    {
        return CommandResult.Fail(
            reason: $"Use `/inventory value` instead",
            explaination: $"This message command has been deprecated and replaced with a Slash Command. [Discord plans to remove support for message commands in April 2022](https://support-dev.discord.com/hc/en-us/articles/4404772028055)."
        );
    }
}
