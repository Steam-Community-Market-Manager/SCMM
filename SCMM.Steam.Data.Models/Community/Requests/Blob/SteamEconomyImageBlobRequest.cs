using System;

namespace SCMM.Steam.Data.Models.Community.Requests.Blob
{
    public class SteamEconomyImageBlobRequest : SteamRequest
    {
        public SteamEconomyImageBlobRequest() { }
        public SteamEconomyImageBlobRequest(string id)
        {
            Id = id;
        }

        public string Id { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamCommunityAssetUrl}/economy/image/{Uri.EscapeDataString(Id)}"
        );
    }
}
