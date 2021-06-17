using System;

namespace SCMM.Web.Data.Models.Domain.Languages
{
    public class LanguageDetailedDTO : LanguageDTO
    {
        public Guid Id { get; set; }

        public string SteamId { get; set; }

        public string Name { get; set; }
    }
}
