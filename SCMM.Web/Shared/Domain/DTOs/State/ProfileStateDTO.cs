using SCMM.Web.Shared.Domain.DTOs.Currencies;
using SCMM.Web.Shared.Domain.DTOs.Profiles;
using System;

namespace SCMM.Web.Shared.Domain.DTOs
{
    public class ProfileStateDTO : ProfileSummaryDTO
    {
        public Guid Id { get; set; }

        public string SteamId { get; set; }

        public string Country { get; set; }

        public LanguageStateDTO LocalLanguage { get; set; }

        public CurrencyStateDTO LocalCurrency { get; set; }

        public CurrencyStateDTO SystemCurrency { get; set; }
    }
}
