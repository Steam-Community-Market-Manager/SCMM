using SCMM.Shared.Data.Store;
using SCMM.Shared.Data.Store.Types;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Discord.Data.Store
{
    public class DiscordGuild : ConfigurableEntity<ulong, DiscordGuild.GuildConfiguration>
    {
        [Required]
        public string Name { get; set; }

        public GuildFlags Flags { get; set; }

        [Flags]
        public enum GuildFlags : byte
        {
            None = 0x00,
            VIP = 0x01,
            Alerts = 0x02
        }

        protected override IEnumerable<ConfigurationDefinition> ConfigurationDefinitions
            => GuildConfiguration.Definitions;

        [ComplexType]
        public class GuildConfiguration : IConfigurationOption
        {
            public const string Currency = "Currency";

            public const string AlertChannelAppItemDefinitionsUpdated = "Alert-Channel-App-Item-Definitions-Updated";
            public const string AlertChannelMarketItemAdded = "Alert-Channel-Market-Item-Added";
            public const string AlertChannelMarketItemManipulationDetected = "Alert-Channel-Market-Item-Manipulation-Detected";
            public const string AlertChannelMarketItemPriceAllTimeHighReached = "Alert-Channel-Market-Item-Price-All-Time-High-Reached";
            public const string AlertChannelMarketItemPriceAllTimeLowReached = "Alert-Channel-Market-Item-Price-All-Time-Low-Reached";
            public const string AlertChannelMarketItemPriceProfitableDealDetected = "Alert-Channel-Market-Item-Price-Profitable-Deal-Detected";
            public const string AlertChannelStoreItemAdded = "Alert-Channel-Store-Item-Added";
            public const string AlertChannelStoreMediaAdded = "Alert-Channel-Store-Media-Added";
            public const string AlertChannelWorkshopFilePublished = "Alert-Channel-Workshop-File-Published";
            public const string AlertChannelWorkshopFileUpdated = "Alert-Channel-Workshop-File-Updated";

            public GuildConfiguration()
            {
                List = new PersistableStringCollection();
            }

            [Required]
            public string Name { get; set; }

            public string Value { get; set; }

            public PersistableStringCollection List { get; set; }

            ICollection<string> IConfigurationOption.List
            {
                get => this.List;
                set => this.List = new PersistableStringCollection(value);
            }

            public static IEnumerable<ConfigurationDefinition> Definitions => new ConfigurationDefinition[]
            {
                new ConfigurationDefinition()
                {
                    Name = Currency,
                    Description = "The list of currencies used when showing prices",
                    AllowMultipleValues = true,
                    AllowedValues = new [] {
                        "BRL","CL","VND","SGD","IDR","ZAR","THB","CRC","NZD","MXN","KZT","GBP","KWD",
                        "AED","ARS","NOK","PEN","PHP","TRY","AUD","MYR","HKD","UYU","CNY","CHF","UAH",
                        "INR","CAD","JPY","USD","COP","EUR","QAR","PLN","RUB","TWD","SAR","KRW","ILS"
                    }
                },
                new ConfigurationDefinition()
                {
                    Name = AlertChannelAppItemDefinitionsUpdated,
                    Description =  "The channel where alerts will be posted when new items are added to the game files.",
                    RequiredFlags = (int) GuildFlags.Alerts
                },
                new ConfigurationDefinition()
                {
                    Name = AlertChannelMarketItemAdded,
                    Description =  "The channel where alerts will be posted when new items are listed on the community market.",
                    RequiredFlags = (int) GuildFlags.Alerts
                },
                new ConfigurationDefinition()
                {
                    Name = AlertChannelMarketItemManipulationDetected,
                    Description =  "The channel where alerts will be posted when market items appear to be undergoing manipulation on the community market.",
                    RequiredFlags = (int) GuildFlags.Alerts
                },
                new ConfigurationDefinition()
                {
                    Name = AlertChannelMarketItemPriceAllTimeHighReached,
                    Description =  "The channel where alerts will be posted when items hit their all-time-highest price on the community market.",
                    RequiredFlags = (int) GuildFlags.Alerts
                },
                new ConfigurationDefinition()
                {
                    Name = AlertChannelMarketItemPriceAllTimeLowReached,
                    Description =  "The channel where alerts will be posted when items hit their all-time-lowest price on the community market.",
                    RequiredFlags = (int) GuildFlags.Alerts
                },
                new ConfigurationDefinition()
                {
                    Name = AlertChannelMarketItemPriceProfitableDealDetected,
                    Description =  "The channel where alerts will be posted when cheap deals are detected on third-party markets.",
                    RequiredFlags = (int) GuildFlags.Alerts
                },
                new ConfigurationDefinition()
                {
                    Name = AlertChannelStoreItemAdded,
                    Description =  "The channel where alerts will be posted when new items are added to the in-game store.",
                    RequiredFlags = (int) GuildFlags.Alerts
                },
                new ConfigurationDefinition()
                {
                    Name = AlertChannelStoreMediaAdded,
                    Description =  "The channel where alerts will be posted when a new video is posted about the current item store.",
                    RequiredFlags = (int) GuildFlags.Alerts
                },
                new ConfigurationDefinition()
                {
                    Name = AlertChannelWorkshopFilePublished,
                    Description =  "The channel where alerts will be posted when new items are submitted to the workshop.",
                    RequiredFlags = (int) GuildFlags.Alerts
                },
                new ConfigurationDefinition()
                {
                    Name = AlertChannelWorkshopFileUpdated,
                    Description =  "The channel where alerts will be posted when a previously accepted workshop item gets updated in-game.",
                    RequiredFlags = (int) GuildFlags.Alerts
                }
            };
        }
    }
}
