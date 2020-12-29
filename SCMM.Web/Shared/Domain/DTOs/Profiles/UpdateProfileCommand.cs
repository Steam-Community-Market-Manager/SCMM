namespace SCMM.Web.Shared.Domain.DTOs.Profiles
{
    public class UpdateProfileCommand
    {
        public string DiscordId { get; set; }

        public string TradeUrl { get; set; }

        public string Language { get; set; }

        public string Currency { get; set; }

        public long? GamblingOffset { get; set; }
    }
}
