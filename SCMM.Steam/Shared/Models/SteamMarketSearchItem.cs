using Newtonsoft.Json;

namespace SCMM.Steam.Shared.Models
{
    public class SteamMarketSearchItem
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("hash_name")]
        public string HashName { get; set; }

        [JsonProperty("id_name")]
        public string IdName { get; set; }

        [JsonProperty("asset_description")]
        public SteamAssetDescription AssetDescription { get; set; }

        [JsonProperty("sell_listings")]
        public int SellListings { get; set; }

        [JsonProperty("sell_price")]
        public int SellPrice { get; set; }
    }
}
