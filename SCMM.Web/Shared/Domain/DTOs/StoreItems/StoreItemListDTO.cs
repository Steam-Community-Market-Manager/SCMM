using System;
using System.Collections.Generic;

namespace SCMM.Web.Shared.Domain.DTOs.StoreItems
{
    public class StoreItemListDTO
    {
        public string SteamId { get; set; }

        public string SteamAppId { get; set; }

        public string SteamWorkshopId { get; set; }

        public string Name { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public CurrencyDTO Currency { get; set; }

        public int StorePrice { get; set; }

        public int MarketRankPosition { get; set; }

        public int MarketRankTotal { get; set; }

        public DateTimeOffset AcceptedOn { get; set; }

        public IDictionary<string, double> SubscriptionsHistory { get; set; }

        public int Subscriptions { get; set; }

        public int Favourited { get; set; }

        public int Views { get; set; }

        public IDictionary<string, string> Tags { get; set; }
    }
}
