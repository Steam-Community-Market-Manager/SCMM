using SCMM.Shared.Data.Store;

namespace SCMM.Steam.Data.Store
{
    public class DiscordConfiguration : Configuration
    {
        public const string Currency = "Currency";
        public const string AlertChannel = "Alert-Channel";
        public const string AlertsStore = "Alerts-Store";
        public const string AlertsMarket = "Alerts-Market";
        public const string AlertsWorkshop = "Alerts-Workshop";
        public const string AlertsItemDefinition = "Alerts-Item-Definition";

        public static IEnumerable<ConfigurationDefinition> Definitions => new ConfigurationDefinition[]
        {
            new ConfigurationDefinition()
            {
                Name = Currency,
                Description = "The list of currencies displayed in notifications",
                AllowMultipleValues = true,
                AllowedValues = new [] {
                    "BRL","CL","VND","SGD","IDR","ZAR","THB","CRC","NZD","MXN","KZT","GBP","KWD",
                    "AED","ARS","NOK","PEN","PHP","TRY","AUD","MYR","HKD","UYU","CNY","CHF","UAH",
                    "INR","CAD","JPY","USD","COP","EUR","QAR","PLN","RUB","TWD","SAR","KRW","ILS"
                }
            },
            new ConfigurationDefinition()
            {
                Name = AlertChannel,
                Description =  "The channel where notifications will be posted.",
            },
            new ConfigurationDefinition()
            {
                Name = AlertsStore,
                Description =  "Show a notification when new items are released to the Steam Store.",
                AllowedValues = new [] {
                    bool.TrueString, bool.FalseString
                }
            },
            new ConfigurationDefinition()
            {
                Name = AlertsMarket,
                Description =  "Show a notification when new items are released to the Steam Community Market.",
                AllowedValues = new [] {
                    bool.TrueString, bool.FalseString
                }
            },
            new ConfigurationDefinition()
            {
                Name = AlertsWorkshop,
                Description =  "Show a notification when new items are accepted in-game from the Steam Workshop.",
                AllowedValues = new [] {
                    bool.TrueString, bool.FalseString
                }
            },
            new ConfigurationDefinition()
            {
                Name = AlertsItemDefinition,
                Description =  "Show a notification when item definitions are updated in the game manifest.",
                AllowedValues = new [] {
                    bool.TrueString, bool.FalseString
                }
            }
        };
    }
}
