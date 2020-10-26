using SCMM.Web.Server.Data.Types;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Web.Server.Data.Models.Discord
{
    public class DiscordConfiguration : Entity
    {
        public const string CurrencyDefault = "Currency.Default";
        public const string CurrencyList = "Currency.List";
        public const string AlertChannel = "Alert.Channel";
        public const string Alert = "Alert.{0}";

        public DiscordConfiguration()
        {
            List = new PersistableStringCollection();
        }

        [Required]
        public string Name { get; set; }

        public string Value { get; set; }

        public PersistableStringCollection List { get; set; }
    }
}
