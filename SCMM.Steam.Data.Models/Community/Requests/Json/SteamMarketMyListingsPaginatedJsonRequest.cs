namespace SCMM.Steam.Data.Models.Community.Requests.Json
{
    public class SteamMarketMyListingsPaginatedJsonRequest : SteamCommunityPaginatedJsonRequest
    {
        public const int MaxPageSize = 100;

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/market/mylistings?start={Start}&count={Count}&language={Uri.EscapeDataString(Language)}&currency={Uri.EscapeDataString(CurrencyId)}&norender={(NoRender ? "1" : "0")}"
        );
    }
}
