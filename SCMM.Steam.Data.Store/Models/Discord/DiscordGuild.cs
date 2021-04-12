using SCMM.Steam.Data.Models.Enums;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store.Models.Discord
{
    public class DiscordGuild : ConfigurableEntity<DiscordConfiguration>
    {
        public DiscordGuild()
        {
            BadgeDefinitions = new Collection<DiscordBadgeDefinition>();
        }

        [Required]
        public string DiscordId { get; set; }

        [Required]
        public string Name { get; set; }

        public DiscordGuildFlags Flags { get; set; }

        protected override IEnumerable<ConfigurationDefinition> ConfigurationDefinitions
            => DiscordConfiguration.Definitions;

        public ICollection<DiscordBadgeDefinition> BadgeDefinitions { get; set; }

    }
}
