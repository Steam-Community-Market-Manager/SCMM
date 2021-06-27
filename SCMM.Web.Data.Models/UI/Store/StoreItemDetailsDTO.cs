using System;
using System.Collections.Generic;

namespace SCMM.Web.Data.Models.UI.Store
{
    public class StoreItemDetailsDTO : IItemDescription, ICanBeSubscribed
    {
        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public ulong? AssetDescriptionId { get; set; }

        public ulong? WorkshopFileId { get; set; }

        public bool HasWorkshopFile => (WorkshopFileId != null);

        public string MarketListingId { get; set; }

        public bool HasMarketListing => !String.IsNullOrEmpty(MarketListingId);

        public string AuthorName { get; set; }

        public string AuthorAvatarUrl { get; set; }

        public string ItemType { get; set; }

        public string ItemCollection { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public IDictionary<string, string> Tags { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public long? StorePrice { get; set; }

        public int? TopSellerIndex { get; set; }

        public bool IsStillAvailableFromStore { get; set; }

        public long? MarketPrice { get; set; }

        public int MarketRankIndex { get; set; }

        public int MarketRankTotal { get; set; }

        public long? Subscriptions { get; set; }

        public long? SalesMinimum { get; set; }

        public long? SalesMaximum { get; set; }

        public bool IsMarketable { get; set; }

        public int? MarketableRestrictionDays { get; set; }

        public bool IsTradable { get; set; }

        public int? TradableRestrictionDays { get; set; }

        public bool IsBreakable { get; set; }

        public IDictionary<string, uint> BreaksIntoComponents { get; set; }

        public bool IsDraft { get; set; }
    }
}
