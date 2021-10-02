using SCMM.Steam.Data.Models.Enums;
using SCMM.Web.Data.Models.UI.Item;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Statistic
{
    public class ItemActivityStatisticDTO : ItemDescriptionDTO
    {
        public DateTimeOffset Timestamp { get; set; }

        public SteamMarketItemActivityType Type { get; set; }

        public long Price { get; set; }

        public long Quantity { get; set; }

        public string SellerName { get; set; }

        public string SellerAvatarUrl { get; set; }

        public string BuyerName { get; set; }

        public string BuyerAvatarUrl { get; set; }

        [JsonIgnore]
        public bool IsSale => (!string.IsNullOrEmpty(BuyerName) && !string.IsNullOrEmpty(SellerName));
    }
}
