namespace SCMM.Steam.Data.Models.Community.Requests.Html
{
    public class SteamProfileInventoryPageRequest : SteamRequest
    {
        public string SteamId { get; set; }

        public string AppId { get; set; }

        public string AssetId { get; set; }

        public override Uri Uri => new Uri(string.IsNullOrEmpty(AssetId)
            ? $"{Constants.SteamCommunityUrl}/profiles/{Uri.EscapeDataString(SteamId)}/inventory/#{Uri.EscapeDataString(AppId)}"
            : $"{Constants.SteamCommunityUrl}/profiles/{Uri.EscapeDataString(SteamId)}/inventory/#{Uri.EscapeDataString(AppId)}_2_{AssetId}"
        );
    }
}
