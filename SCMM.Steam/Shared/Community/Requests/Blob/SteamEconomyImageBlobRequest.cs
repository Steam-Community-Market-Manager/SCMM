using System;

namespace SCMM.Steam.Shared.Community.Requests.Blob
{
    public class SteamEconomyImageBlobRequest : SteamRequest
    {
        public SteamEconomyImageBlobRequest() { }
        public SteamEconomyImageBlobRequest(string iconId)
        {
            IconId = iconId;
        }

        public string IconId { get; set; }

        public Uri Uri => new Uri(
            $"{SteamConstants.SteamCommunityAssetUrl}/economy/image/{Uri.EscapeDataString(IconId)}"
        );
    }
}
