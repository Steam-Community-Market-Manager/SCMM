using Discord;
using Discord.Commands;

namespace SCMM.Discord.Client.Commands
{
    public class CommandResult : RuntimeResult
    {
        public CommandResult(CommandError? error = null, Emoji reaction = null, Embed embed = null, string message = null, string explaination = null, string helpUrl = null, string helpImageUrl = null) : base(error, message)
        {
            Reaction = reaction;
            Embed = embed;
            Explaination = explaination;
            HelpUrl = helpUrl;
            HelpImageUrl = helpImageUrl;
        }

        public Emoji Reaction { get; set; }

        public Embed Embed { get; set; }

        public string Explaination { get; set; }

        public string HelpUrl { get; set; }

        public string HelpImageUrl { get; set; }

        public static CommandResult Success(string message = null, Embed embed = null, Emoji reaction = null)
        {
            return new CommandResult(reaction: reaction ?? new Emoji("👌") /* ok */, message: message, embed: embed);
        }

        public static CommandResult Fail(string reason, string explaination = null, string helpUrl = null, string helpImageUrl = null, Emoji reaction = null)
        {
            return new CommandResult(error: CommandError.Unsuccessful, reaction: reaction ?? new Emoji("😢") /* cry */, message: reason, explaination: explaination, helpUrl: helpUrl, helpImageUrl: helpImageUrl);
        }
    }
}
