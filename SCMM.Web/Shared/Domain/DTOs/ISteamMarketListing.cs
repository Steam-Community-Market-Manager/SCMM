namespace SCMM.Web.Shared.Domain.DTOs.InventoryItems
{
    public interface ISteamMarketListing
    {
        public string SteamAppId { get; }

        public string SteamId { get; }

        public string Name { get; }
    }
}
