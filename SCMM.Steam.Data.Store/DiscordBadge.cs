using SCMM.Data.Shared.Store;
using System;
using System.ComponentModel.DataAnnotations;

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
