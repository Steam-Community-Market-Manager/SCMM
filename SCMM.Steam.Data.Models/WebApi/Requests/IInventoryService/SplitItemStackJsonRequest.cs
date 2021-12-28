namespace SCMM.Steam.Data.Models.WebApi.Requests.IInventoryService
{
    /// <summary>
    /// https://steamapi.xpaw.me/#IInventoryService/SplitItemStack
    /// </summary>
    public class SplitItemStackJsonRequest : SteamRequest
    {
        public string Key { get; set; }

        public ulong AppId { get; set; }

        public ulong SteamId { get; set; }

        public ulong ItemId { get; set; }

        public uint Quantity { get; set; }

        public override Uri Uri => new Uri(
            $"{Constants.SteamWebApiUrl}/IInventoryService/CombineItemStacks/v1/?key={Uri.EscapeDataString(Key)}&appid={AppId}&steamId={SteamId}&itemid={ItemId}&quantity={Quantity}"
        );
    }
}
