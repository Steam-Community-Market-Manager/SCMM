namespace SCMM.Steam.Data.Models.WebApi.Requests.IInventoryService
{
    /// <summary>
    /// https://steamapi.xpaw.me/#IInventoryService/CombineItemStacks
    /// </summary>
    public class CombineItemStacksJsonRequest : SteamRequest
    {
        public string Key { get; set; }

        public ulong AppId { get; set; }

        public ulong SteamId { get; set; }

        public ulong FromItemId { get; set; }

        public ulong DestItemId { get; set; }

        public uint Quantity { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamWebApiUrl}/IInventoryService/CombineItemStacks/v1/?key={Uri.EscapeDataString(Key)}&appid={AppId}&steamId={SteamId}&fromitemid={FromItemId}&destitemid={DestItemId}&quantity={Quantity}"
        );
    }
}
