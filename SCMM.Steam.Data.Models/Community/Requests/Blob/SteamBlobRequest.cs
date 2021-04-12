using System;

namespace SCMM.Steam.Data.Models.Community.Requests.Blob
{
    public class SteamBlobRequest : SteamRequest
    {
        private readonly string _url;

        public SteamBlobRequest(string url)
        {
            _url = url;
        }

        public override Uri Uri => new Uri(_url);
    }
}
