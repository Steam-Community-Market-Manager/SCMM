namespace SCMM.Steam.Data.Models.WebApi.Requests.ISteamEconomy
{
    /// <summary>
    /// https://steamapi.xpaw.me/#ISteamEconomy/GetAssetClassInfo
    /// </summary>
    public class GetAssetClassInfoJsonRequest : SteamRequest
    {
        public string Key { get; set; }

        public ulong AppId { get; set; }

        public string Language { get; set; }

        public ulong[] ClassIds { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamWebApiUrl}/ISteamEconomy/GetAssetClassInfo/v1/?key={Uri.EscapeDataString(Key)}&appId={AppId}&language={Uri.EscapeDataString(Language ?? String.Empty)}{BuildClassIdParameters()}"
        );

        private string BuildClassIdParameters()
        {
            var classes = $"&class_count={ClassIds?.Length ?? 0}";
            if (ClassIds != null)
            {
                for (int i = 0; i < ClassIds.Length; i++)
                {
                    classes += $"&classid{i}={ClassIds[i]}";
                }
            }

            return classes;
        }
    }
}
