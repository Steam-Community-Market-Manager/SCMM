using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store.Models.Discord
{
    public class DiscordBadge : Entity
    {
        [Required]
        public string DiscordUserId { get; set; }

        [Required]
        public Guid BadgeDefinitionId { get; set; }

        public DiscordBadgeDefinition BadgeDefinition { get; set; }
    }
}
