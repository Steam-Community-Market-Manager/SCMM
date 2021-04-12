namespace SCMM.Web.Data.Models.Steam
{
    public interface ISteamMarketListing
    {
        public string SteamAppId { get; }

        public string SteamId { get; }

        public string Name { get; }
    }
}
