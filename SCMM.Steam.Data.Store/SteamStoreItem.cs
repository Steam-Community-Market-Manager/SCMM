using SCMM.Steam.Data.Store.Types;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store
{
    public class SteamStoreItem : SteamItem
    {
        public SteamStoreItem()
        {
            Stores = new Collection<SteamStoreItemItemStore>();
            Prices = new PersistablePriceDictionary();
            SubscriberTimeline = new Collection<SubscriberSnapshot>();
        }

        public Guid? CurrencyId { get; private set; }

        public SteamCurrency Currency { get; private set; }

        /// <summary>
        /// The most recent price this item was sold for on the store
        /// </summary>
        public long? Price { get; private set; }

        /// <summary>
        /// The most recent price set this item was sold for on the store.
        /// Store prices are generally fixed and don't fluctuate with currency exchange rates.
        /// Because of this, we need to keep a list of all the fixed store prices in each currency.
        /// </summary>
        [Required]
        public PersistablePriceDictionary Prices { get; private set; }

        /// <summary>
        /// If true, there is at least one associated store that can be purchased from. Otherwise, this item isn't available for purchase currently.
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// If true, this item has returned from a previous store release
        /// </summary>
        public bool HasReturnedToStore { get; set; }

        public ICollection<SteamStoreItemItemStore> Stores { get; set; }

        public ICollection<SubscriberSnapshot> SubscriberTimeline { get; set; }

        public void UpdatePrice(SteamCurrency currency, long price, PersistablePriceDictionary prices)
        {
            CurrencyId = currency?.Id;
            Currency = currency;
            Price = price;
            Prices = new PersistablePriceDictionary(prices);
        }

        public void UpdateLatestPrice()
        {
            var latestStore = Stores?.FirstOrDefault(x => x.Store?.Start == Stores?.Max(x => x.Store?.Start));
            if (latestStore != null)
            {
                CurrencyId = latestStore.CurrencyId;
                Currency = latestStore.Currency;
                Price = latestStore.Price;
                Prices = new PersistablePriceDictionary(latestStore.Prices);
            }

            RecalculateHasReturnedToStore();
        }

        public void RecalculateHasReturnedToStore()
        {
            if (!Stores.Any(x => x.Store != null))
            {
                return;
            }

            // If the item doesn't belong to any fixed period stores, it can't have returned before
            var limitedStores = Stores.Where(x => x.Store.Start != null).ToList();
            if (!limitedStores.Any())
            {
                return;
            }

            // Calculate the different between time in store vs. time in circulation to see if there was any period where the item was not available
            var firstTimeSeen = limitedStores.Min(x => x.Store.Start.Value);
            var lastTimeSeen = limitedStores.Max(x => (x.Store.End ?? x.Store.Start.Value.AddDays(7)));
            var totalTimeInStore = limitedStores.Select(x => ((x.Store.End ?? x.Store.Start.Value.AddDays(7)) - x.Store.Start.Value)).Aggregate(TimeSpan.Zero, (t1, t2) => t1 + t2);
            var totalTimeInExistance = (lastTimeSeen - firstTimeSeen).Subtract(TimeSpan.FromHours(1));
            HasReturnedToStore = (totalTimeInExistance > totalTimeInStore);
        }

        public class SubscriberSnapshot
        {
            public DateTimeOffset Timestamp { get; set; }

            public ulong Subscribers { get; set; }
        }
    }
}
