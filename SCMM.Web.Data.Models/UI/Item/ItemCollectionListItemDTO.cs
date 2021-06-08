using SCMM.Web.Data.Models.Domain.Currencies;
using System;

namespace SCMM.Web.Data.Models.Domain.Item
{
    public class ItemCollectionListItemDTO
    {
        public ulong Id { get; set; }

        public ulong AppId { get; set; }

        public string AppName { get; set; }

        public ulong? WorkshopFileId { get; set; }

        public bool HasWorkshopFile => (WorkshopFileId != null);

        public string StoreItemId { get; set; }

        public bool HasStoreItem => (StoreItemId != null);

        public string MarketListingId { get; set; }

        public bool HasMarketListing => !String.IsNullOrEmpty(MarketListingId);

        public string Name { get; set; }

        public string BackgroundColour { get; set; }

        public string ForegroundColour { get; set; }

        public string IconUrl { get; set; }

        public CurrencyDTO Currency { get; set; }

        public long? StorePrice { get; set; }

        public long? MarketPrice { get; set; }

        public string ItemType { get; set; }
    }
}
