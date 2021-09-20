using System.Text.Json.Serialization;

namespace SCMM.Steam.Data.Models.Community.Models
{
    public class SteamMarketSearchItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("hash_name")]
        public string HashName { get; set; }

        [JsonPropertyName("id_name")]
        public string IdName { get; set; }

        [JsonPropertyName("asset_description")]
        public SteamAssetClass AssetDescription { get; set; }

        [JsonPropertyName("sell_listings")]
        public int SellListings { get; set; }

        [JsonPropertyName("sell_price")]
        public int SellPrice { get; set; }
    }
}
