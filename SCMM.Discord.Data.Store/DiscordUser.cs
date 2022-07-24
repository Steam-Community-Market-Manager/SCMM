using SCMM.Shared.Data.Store;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Discord.Data.Store
{
    public class DiscordUser : Entity
    {
        [Required]
        public ulong Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Discriminator { get; set; }

        public string FullUsername => $"{Username}#{Discriminator}";

        public string CurrencyId { get; set; }

        public string SteamId { get; set; }
    }
}
