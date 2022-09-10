using SCMM.Shared.Data.Store;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Discord.Data.Store
{
    public class DiscordUser : Entity<ulong>
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Discriminator { get; set; }

        public string CurrencyId { get; set; }

        public string SteamId { get; set; }

        public string GetFullUsername() => $"{Username}#{Discriminator}";
    }
}
