using SCMM.Web.Server.Data.Models.Discord;
using System;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Data.Models
{
    public class DiscordBadgeDefinition : Entity
    {
        [Required]
        public Guid GuildId { get; set; }

        public DiscordGuild Guild { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public Guid IconId { get; set; }

        public ImageData Icon { get; set; }
    }
}
