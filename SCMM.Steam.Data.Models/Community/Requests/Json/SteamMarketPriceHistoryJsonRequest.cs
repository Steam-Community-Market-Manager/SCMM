namespace SCMM.Steam.Data.Models.Community.Requests.Json
{
    /// <remarks>This API requires authentication</remarks>
    public class SteamMarketPriceHistoryJsonRequest : SteamRequest
    {
        public string AppId { get; set; }

        public string MarketHashName { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/market/pricehistory?appid={Uri.EscapeDataString(AppId)}&market_hash_name={Uri.EscapeDataString(MarketHashName)}"
        );
    }
}
