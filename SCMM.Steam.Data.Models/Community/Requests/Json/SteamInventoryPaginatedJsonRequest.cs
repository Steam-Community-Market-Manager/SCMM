using System;

namespace SCMM.Steam.Data.Models.Community.Requests.Json
{
    public class SteamInventoryPaginatedJsonRequest : SteamPaginatedJsonRequest
    {
        public const int MaxPageSize = 5000;

        public string SteamId { get; set; }

        public string AppId { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityUrl}/inventory/{Uri.EscapeDataString(SteamId)}/{Uri.EscapeDataString(AppId)}/2?start={Start}&count={Count}&norender={(NoRender ? "1" : "0")}"
        );
    }
}
