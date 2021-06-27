using SCMM.Shared.Data.Store;
using SCMM.Steam.Data.Models.Enums;
using System;

namespace SCMM.Steam.Data.Store
{
    public class SteamMarketItemActivity : Entity
    {
        public DateTimeOffset Timestamp { get; set; }

        public Guid? DescriptionId { get; set; }

        public SteamAssetDescription Description { get; set; }

        public Guid ItemId { get; set; }

        public SteamMarketItem Item { get; set; }

        public Guid? CurrencyId { get; set; }

        public SteamCurrency Currency { get; set; }

        public SteamMarketItemActivityType Type { get; set; }

        public long Price { get; set; }

        public long Quantity { get; set; }

        public string SellerName { get; set; }

        public string SellerAvatarUrl { get; set; }

        public string BuyerName { get; set; }

        public string BuyerAvatarUrl { get; set; }
    }
}
