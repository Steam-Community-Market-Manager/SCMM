using System;

namespace SCMM.Steam.Shared.Requests
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
            $"{SteamConstants.SteamAssetUrl}/economy/image/{Uri.EscapeDataString(IconId)}"
        );
    }
}
