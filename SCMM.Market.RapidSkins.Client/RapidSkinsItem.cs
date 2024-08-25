using System.Text.Json.Serialization;

namespace SCMM.Market.RapidSkins.Client;

public class RapidSkinsItem
{
    [JsonPropertyName("ownerSteamId")]
    public string OwnerSteamId { get; set; }

    [JsonPropertyName("appId")]
    public ulong AppId { get; set; }

    [JsonPropertyName("marketHashName")]
    public string MarketHashName { get; set; }

    [JsonPropertyName("price")]
    public ItemPrice Price { get; set; }

    [JsonPropertyName("stack")]
    public ItemStack[] Stack { get; set; }

    public class ItemPrice
    {
        [JsonPropertyName("coinAmount")]
        public long CoinAmount { get; set; }
    }

    public class ItemStack
    {
        [JsonPropertyName("assetId")]
        public string AssetId { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }
    }
}
