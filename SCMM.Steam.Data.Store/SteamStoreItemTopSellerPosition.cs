using SCMM.Shared.Data.Store;

namespace SCMM.Steam.Data.Store
{
    public class SteamStoreItemTopSellerPosition : Entity
    {
        public DateTimeOffset Timestamp { get; set; }

        public Guid? DescriptionId { get; set; }

        public SteamAssetDescription Description { get; set; }

        public int Position { get; set; }

        public int Total { get; set; }

        public TimeSpan Duration { get; set; }
    }
}
