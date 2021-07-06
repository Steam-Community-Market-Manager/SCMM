using SCMM.Steam.Data.Models.Enums;
using SCMM.Web.Data.Models.UI.Item;
using System;

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

        public bool IsSale => (!String.IsNullOrEmpty(BuyerName) && !String.IsNullOrEmpty(SellerName));
    }
}
