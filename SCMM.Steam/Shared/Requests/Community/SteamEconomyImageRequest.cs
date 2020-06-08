using System;

namespace SCMM.Steam.Shared.Requests.Community
{
    public class SteamEconomyImageRequest : SteamRequest
    {
        public SteamEconomyImageRequest() { }
        public SteamEconomyImageRequest(string iconId)
        {
            IconId = iconId;
        }

        public string IconId { get; set; }

        public Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityAssetUrl}/economy/image/{Uri.EscapeDataString(IconId)}"
        );
    }
}
