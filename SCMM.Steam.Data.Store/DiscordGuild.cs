using SCMM.Shared.Data.Store;
using SCMM.Steam.Data.Models.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store
{
    public class DiscordGuild : ConfigurableEntity<DiscordConfiguration>
    {
        [Required]
        public string DiscordId { get; set; }

        [Required]
        public string Name { get; set; }

        public DiscordGuildFlags Flags { get; set; }

        protected override IEnumerable<ConfigurationDefinition> ConfigurationDefinitions
            => DiscordConfiguration.Definitions;
    }
}
