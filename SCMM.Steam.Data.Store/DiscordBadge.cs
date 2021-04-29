using System;
using System.ComponentModel.DataAnnotations;
using SCMM.Shared.Data.Store;

namespace SCMM.Steam.Data.Store
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
