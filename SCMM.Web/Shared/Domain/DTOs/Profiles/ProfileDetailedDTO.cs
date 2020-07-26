using SCMM.Web.Shared.Domain.DTOs.Languages;
using SCMM.Web.Shared.Domain.DTOs.Currencies;
using System;

namespace SCMM.Web.Shared.Domain.DTOs.Profiles
{
    public class ProfileDetailedDTO : ProfileDTO
    {
        public Guid Id { get; set; }

        public string Country { get; set; }

        public LanguageDetailedDTO Language { get; set; }

        public CurrencyDetailedDTO Currency { get; set; }
    }
}
