using SCMM.Steam.Data.Models.Enums;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemDistributionDTO
    {
        [JsonIgnore]
        public long TotalItemCount => ((EstimatedItemCount ?? 0) + KnownItemCount);

        public long? EstimatedItemCount { get; set; }

        [JsonIgnore]
        public long? UnknownItemCount => (EstimatedItemCount - KnownItemCount);

        [JsonIgnore]
        public bool HasUnknownItems => (UnknownItemCount ?? 0) > 0;

        [JsonIgnore]
        public long KnownItemCount => (KnownInventoryItemCount + KnownMarketItemCounts.Sum(x => x.Value));

        public long KnownInventoryItemCount { get; set; }

        public IDictionary<MarketType, long> KnownMarketItemCounts { get; set; }
    }
}
