using System;

namespace SCMM.Web.Data.Models.UI.Language
{
    public class LanguageDetailedDTO : LanguageDTO
    {
        public Guid Id { get; set; }

        public string SteamId { get; set; }

        public string Name { get; set; }
    }
}
