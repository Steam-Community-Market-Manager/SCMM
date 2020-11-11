using System.Collections.Generic;

namespace SCMM.Web.Server.Data.Models.Discord
{
    public class DiscordConfiguration : Configuration
    {
        public const string Currency = "Currency";
        public const string Alerts = "Alerts";
        public const string AlertsStore = "Store";
        public const string AlertsMarket = "Market";
        public const string AlertsWorkshop = "Workshop";
        public const string AlertChannel = "Alert-Channel";

        public static IEnumerable<ConfigurationDefinition> Definitions => new ConfigurationDefinition[]
        {
            new ConfigurationDefinition()
            {
                Name = Currency,
                Description = "Set the list of currencies displayed in notifications with the `add` or `remove` commands. Set the default currency used for commands with the `set` command.",
                AllowedValues = new [] { 
                    "BRL","CL","VND","SGD","IDR","ZAR","THB","CRC","NZD","MXN","KZT","GBP","KWD",
                    "AED","ARS","NOK","PEN","PHP","TRY","AUD","MYR","HKD","UYU","CNY","CHF","UAH",
                    "INR","CAD","JPY","USD","COP","EUR","QAR","PLN","RUB","TWD","SAR","KRW","ILS" 
                }
            },
            new ConfigurationDefinition()
            {
                Name = Alerts,
                Description =  "Set the type of alerts you want to receive with the `add` or `remove` commands.",
                AllowedValues = new [] {
                    AlertsStore, AlertsMarket, AlertsWorkshop
                }
            },
            new ConfigurationDefinition()
            {
                Name = AlertChannel,
                Description =  "Set the name of the channel that all alerts will be posted to.",
            }
        };
    }
}
