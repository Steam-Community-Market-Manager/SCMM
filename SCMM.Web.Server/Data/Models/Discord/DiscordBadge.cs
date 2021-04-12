using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Data.Models.Discord
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
