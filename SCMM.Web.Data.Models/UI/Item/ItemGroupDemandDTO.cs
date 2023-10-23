using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemGroupDemandDTO
    {
        public long TotalItems { get; set; }

        public long Last24hrMedianPrice { get; set; }

        public long Last168hrMedianPrice { get; set; }

        [JsonIgnore]
        public long Last168hrMedianPriceDelta => (Last168hrMedianPrice - Last24hrMedianPrice);

        public long Last24hrMedianMovementFromStorePrice { get; set; }

        public long Last168hrMedianMovementFromStorePrice { get; set; }

        [JsonIgnore]
        public long Last168hrMedianMovementFromStorePriceDelta => (Last168hrMedianMovementFromStorePrice - Last24hrMedianMovementFromStorePrice);

        public long TotalMarketSupply { get; set; }

        public long MedianMarketSupply { get; set; }

        public long TotalMarketDemand { get; set; }

        public long MedianMarketDemand { get; set; }
    }
}
