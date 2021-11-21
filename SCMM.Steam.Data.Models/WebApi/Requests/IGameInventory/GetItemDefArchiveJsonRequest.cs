namespace SCMM.Steam.Data.Models.WebApi.Requests.IGameInventory
{
    /// <summary>
    /// https://steamapi.xpaw.me/#IGameInventory/GetItemDefArchive
    /// </summary>
    public class GetItemDefArchiveJsonRequest : SteamRequest
    {
        public string Key { get; set; }

        public ulong AppId { get; set; }

        public string Digest { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamWebApiUrl}/IGameInventory/GetItemDefArchive/v1/?key={Uri.EscapeDataString(Key)}&appid={AppId}&digest={Uri.EscapeDataString(Digest)}"
        );
    }
}
