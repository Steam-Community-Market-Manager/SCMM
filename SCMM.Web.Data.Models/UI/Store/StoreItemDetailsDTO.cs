using SCMM.Web.Data.Models.Domain.Currencies;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SCMM.Web.Data.Models.UI.Store
{
    public class StoreItemDetailsDTO : ISearchable, IItemDescription
    {
        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public string AppName { get; set; }

        public ulong? WorkshopFileId { get; set; }

        public bool HasWorkshopFile => (WorkshopFileId != null);

        public string MarketListingId { get; set; }

        public bool HasMarketListing => !String.IsNullOrEmpty(MarketListingId);

        public string AuthorName { get; set; }

        public string AuthorAvatarUrl { get; set; }

        public string ItemType { get; set; }

        public string ItemCollection { get; set; }

        public string Name { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public CurrencyDTO Currency { get; set; }
        
        public long StorePrice { get; set; }

        public int StoreIndex { get; set; }

        public bool IsStillAvailableInStore { get; set; }

        public long? MarketPrice { get; set; }

        public int MarketPriceRankPosition { get; set; }

        public int MarketPriceRankTotal { get; set; }

        public long? Subscriptions { get; set; }

        public long SalesMinimum { get; set; }

        public long? SalesMaximum { get; set; }

        public bool IsMarketable { get; set; }

        public int? MarketableRestrictionDays { get; set; }

        public bool IsTradable { get; set; }

        public int? TradableRestrictionDays { get; set; }

        public bool IsBreakable { get; set; }

        public IDictionary<string, uint> BreaksIntoComponents { get; set; }

        [JsonIgnore]
        public object[] SearchData => new object[] { Id, AuthorName, Name, ItemType };
    }
}
