using System;

namespace SCMM.Web.Shared.Domain.DTOs.Currencies
{
    public class LanguageStateDTO : LanguageDTO
    {
        public Guid Id { get; set; }

        public string SteamId { get; set; }
    }
}
