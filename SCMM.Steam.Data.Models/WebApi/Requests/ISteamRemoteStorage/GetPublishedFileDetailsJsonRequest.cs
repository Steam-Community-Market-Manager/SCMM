using SCMM.Steam.Client;

namespace SCMM.Steam.Data.Models.WebApi.Requests.ISteamRemoteStorage
{
    /// <summary>
    /// https://steamapi.xpaw.me/#ISteamRemoteStorage/GetPublishedFileDetails
    /// </summary>
    public class GetPublishedFileDetailsJsonRequest : SteamFormDataRequest
    {
        public string Key { get; set; }

        public ulong[] PublishedFileIds { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamWebApiUrl}/ISteamRemoteStorage/GetPublishedFileDetails/v1/"
        );

        public override IDictionary<string, string> Data {
            get
            {
                var data = new Dictionary<string, string>();
                if (PublishedFileIds != null)
                {
                    data["itemcount"] = PublishedFileIds.Length.ToString();
                    for (int i = 0; i < PublishedFileIds.Length; i++)
                    {
                        data[$"publishedfileids[{i}]"] = PublishedFileIds[i].ToString();
                    }
                }

                return data;
            }
        }
    }
}
