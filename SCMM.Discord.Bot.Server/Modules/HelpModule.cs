using Discord;
using Discord.Interactions;
using SCMM.Discord.Client;
using SCMM.Discord.Client.Commands;

namespace SCMM.Discord.Bot.Server.Modules;

[Group("help", "Help commands")]
public class HelpModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly DiscordConfiguration _configuration;

    public HelpModule(DiscordConfiguration configuration)
    {
        _configuration = configuration;
    }

    [SlashCommand("invite", "Invite this bot to another Discord server", runMode: RunMode.Sync)]
    public RuntimeResult GetBotInviteAsync()
    {
        var name = Context.Client.CurrentUser.Username;
        return InteractionResult.Success(
            ephemeral: true,
            embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle($"Click to invite {name} to your Discord server")
                .WithDescription($"{name} is an app for analysing Steam Community Market information. Click the link above to invite {name} to your own Discord server!")
                .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl())
                .WithUrl(_configuration.InviteUrl)
                .Build()
        );
    }
}
