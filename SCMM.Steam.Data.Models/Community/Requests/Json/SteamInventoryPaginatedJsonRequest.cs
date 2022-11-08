namespace SCMM.Steam.Data.Models.Community.Requests.Json
{
    public class SteamInventoryPaginatedJsonRequest : SteamRequest
    {
        public const int MaxPageSize = 5000;

        public string SteamId { get; set; }

        public string AppId { get; set; }

        public string Language { get; set; } = Constants.SteamLanguageEnglish;

        public ulong? StartAssetId { get; set; }

        public int Count { get; set; }

        public bool NoRender { get; set; } = true;

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/inventory/{Uri.EscapeDataString(SteamId)}/{Uri.EscapeDataString(AppId)}/2?count={Count}{(StartAssetId > 0 ? $"&start_assetid={StartAssetId}" : null)}&norender={(NoRender ? "1" : "0")}"
        );
    }
}
