using System;

namespace SCMM.Steam.Data.Models.Domain.Languages
{
    public class LanguageListDTO
    {
        public Guid Id { get; set; }

        public string SteamId { get; set; }

        public string Name { get; set; }
    }
}
