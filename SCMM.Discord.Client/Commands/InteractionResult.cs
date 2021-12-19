using Discord;
using Discord.Interactions;

namespace SCMM.Discord.Client.Commands
{
    public class InteractionResult : RuntimeResult
    {
        public InteractionResult(InteractionCommandError? error = null, Embed embed = null, string message = null, string explaination = null, string helpUrl = null, string helpImageUrl = null, bool ephemeral = false) : base(error, message)
        {
            Embed = embed;
            Explaination = explaination;
            HelpUrl = helpUrl;
            HelpImageUrl = helpImageUrl;
            Ephemeral = ephemeral;
        }

        public string Reason => ErrorReason;

        public Embed Embed { get; set; }

        public string Explaination { get; set; }

        public string HelpUrl { get; set; }

        public string HelpImageUrl { get; set; }

        public bool Ephemeral { get; set; }

        public static InteractionResult Success(string message = null, Embed embed = null, bool ephemeral = false)
        {
            message = (message != null || embed != null) ? message : new Emoji("👌").Name /* ok */;
            return new InteractionResult(message: message, embed: embed, ephemeral: ephemeral);
        }

        public static InteractionResult Fail(string reason, string explaination = null, string helpUrl = null, string helpImageUrl = null, bool ephemeral = false)
        {
            reason = (reason != null) ? reason : new Emoji("😢").Name /* cry */;
            return new InteractionResult(error: InteractionCommandError.Unsuccessful, message: reason, explaination: explaination, helpUrl: helpUrl, helpImageUrl: helpImageUrl, ephemeral: ephemeral);
        }
    }
}
