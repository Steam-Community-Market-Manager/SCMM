using SCMM.Web.Shared.Data.Models.Steam;
using SCMM.Web.Shared.Domain.DTOs.Currencies;
using SCMM.Web.Shared.Domain.DTOs.Languages;
using System;

namespace SCMM.Web.Shared.Domain.DTOs.Profiles
{
    public class ProfileDetailedDTO : ProfileDTO
    {
        public Guid Id { get; set; }

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
