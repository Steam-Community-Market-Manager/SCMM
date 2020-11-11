using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Data.Models.Discord
{
    public class DiscordGuild : ConfigurableEntity<DiscordConfiguration>
    {
        [Required]
        public string DiscordId { get; set; }

        [Required]
        public string Name { get; set; }

        protected override IEnumerable<ConfigurationDefinition> ConfigurationDefinitions 
            => DiscordConfiguration.Definitions;
    }
}
