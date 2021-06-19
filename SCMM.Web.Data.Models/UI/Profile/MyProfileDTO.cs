using SCMM.Web.Data.Models.UI.Currency;
using SCMM.Web.Data.Models.UI.Language;

namespace SCMM.Web.Data.Models.UI.Profile
{
    public class MyProfileDTO : ProfileDetailedDTO
    {
        public string DiscordId { get; set; }

        public string AvatarLargeUrl { get; set; }

        public string TradeUrl { get; set; }

        public string Country { get; set; }

        public LanguageDetailedDTO Language { get; set; }

        public CurrencyDetailedDTO Currency { get; set; }

        public int DonatorLevel { get; set; }

        public long GamblingOffset { get; set; }

        public string[] Roles { get; set; }
    }
}
