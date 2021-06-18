using Newtonsoft.Json;
using SCMM.Shared.Data.Models.Extensions;

namespace SCMM.Web.Data.Models.UI.Store
{
    public class StoreChartItemSalesDTO
    {
        public string Name { get; set; }

        public long? Subscriptions { get; set; }

        public long? KnownInventoryDuplicates { get; set; }

        public long? EstimatedOtherDuplicates { get; set; }

        public long Total => ((Subscriptions ?? 0) + (KnownInventoryDuplicates ?? 0) + (EstimatedOtherDuplicates ?? 0));

        [JsonIgnore]
        public string TotalText => $"{Total.ToQuantityString()} sold";
    }
}
