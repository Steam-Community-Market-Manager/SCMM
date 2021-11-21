namespace SCMM.Steam.Data.Models.WebApi.Requests.IInventoryService
{
    /// <summary>
    /// https://steamapi.xpaw.me/#IInventoryService/GetItemDefMeta
    /// </summary>
    public class GetItemDefMetaJsonRequest : SteamRequest
    {
        public string Key { get; set; }

        public ulong AppId { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamWebApiUrl}/IInventoryService/GetItemDefMeta/v1/?key={Uri.EscapeDataString(Key)}&appid={AppId}"
        );
    }
}
