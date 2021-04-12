using SCMM.Web.Data.Models.Domain.DTOs.Currencies;
using System;
using System.Collections.Generic;

namespace SCMM.Web.Data.Models.Domain.DTOs.StoreItems
{
    public class StoreItemDetailDTO
    {
        public string SteamId { get; set; }

        public string SteamAppId { get; set; }

        public string SteamWorkshopId { get; set; }

        public bool HasWorkshopFile => !String.IsNullOrEmpty(SteamWorkshopId);

        public string SteamMarketItemId { get; set; }

        public bool HasMarketListing => !String.IsNullOrEmpty(SteamMarketItemId);

        public string Name { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public string ItemType { get; set; }

        public string AuthorName { get; set; }

        public string AuthorAvatarUrl { get; set; }

        public CurrencyDTO Currency { get; set; }

        public bool IsStillAvailableInStore { get; set; }

        public long StorePrice { get; set; }

        public int StoreIndex { get; set; }

        public IDictionary<string, double> StoreIndexHistory { get; set; }

        public long? MarketPrice { get; set; }

        public long? MarketQuantity { get; set; }

        public int MarketRankPosition { get; set; }

        public int MarketRankTotal { get; set; }

        public int TotalSalesMin { get; set; }

        public int? TotalSalesMax { get; set; }

        public IDictionary<string, double> TotalSalesHistory { get; set; }

        public IDictionary<string, double> SubscriptionsHistory { get; set; }

        public int? Subscriptions { get; set; }

        public int? Favourited { get; set; }

        public int? Views { get; set; }

        public DateTimeOffset? AcceptedOn { get; set; }

        public IDictionary<string, string> Tags { get; set; }
    }
}
