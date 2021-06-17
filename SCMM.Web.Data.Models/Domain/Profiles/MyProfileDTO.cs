using SCMM.Steam.Data.Models.Enums;
using SCMM.Web.Data.Models.Domain.Currencies;
using SCMM.Web.Data.Models.Domain.Languages;

namespace SCMM.Web.Data.Models.Domain.Profiles
{
    public class MyProfileDTO : ProfileDTO
    {
        public string DiscordId { get; set; }

        public string AvatarLargeUrl { get; set; }

        public string TradeUrl { get; set; }

        public string Country { get; set; }

        public LanguageDetailedDTO Language { get; set; }

        public CurrencyDetailedDTO Currency { get; set; }

        public int DonatorLevel { get; set; }

        public long GamblingOffset { get; set; }

        public SteamProfileFlags Flags { get; set; }

        public string[] Roles { get; set; }
    }
}
