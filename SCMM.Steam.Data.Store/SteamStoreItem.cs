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
        }

        public Guid? CurrencyId { get; private set; }

        public SteamCurrency Currency { get; private set; }

        /// <summary>
        /// The most recent price this item was sold for on the store
        /// </summary>
        public long? Price { get; private set; }

        /// <summary>
        /// The most recent price set this item was sold for on the store.
        /// Store prices are generally fixed and don't fluxuate with currency exhange rates.
        /// Because of this, we need to keep a list of all the fixed store prices in each currency.
        /// </summary>
        [Required]
        public PersistablePriceDictionary Prices { get; private set; }

        public long? TotalSalesMin { get; set; }

        public long? TotalSalesMax { get; set; }

        /// <summary>
        /// If true, there is at least one associated store that can be purchased from. Otherwise, this item isn't available for purchase currently.
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// If true, this item has returned from a previous store release
        /// </summary>
        public bool HasReturnedToStore { get; set; }

        public ICollection<SteamStoreItemItemStore> Stores { get; set; }

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

        public void RecalculateTotalSales(SteamItemStore store)
        {
            var mapping = Stores.FirstOrDefault(x => x.Store == store);
            var orderedStoreItems = mapping?.Store?.Items?.OrderBy(x => x.TopSellerIndex)?.Select(x => x.Item)?.ToList();
            if (orderedStoreItems == null)
            {
                return;
            }

            // NOTE: This approach just assumes a 10-20% increase over subscriptions
            var item = orderedStoreItems.FirstOrDefault(x => x.Id == Id);
            var itemIndex = orderedStoreItems.IndexOf(item);
            var itemUniqueSales = (Description?.SubscriptionsLifetime ?? 0);
            var itemDuplicateSales = (long)Math.Floor(itemUniqueSales > 0 ? (itemUniqueSales * (Math.Max(1, 10 - itemIndex) * 0.01m)) : 0);
            var itemTotalSales = (itemUniqueSales + itemDuplicateSales);
            TotalSalesMin = itemTotalSales;
            TotalSalesMax = null;

            // NOTE: This approach calculates the revenue earned based on subscribers and the position in the top sellers list
            // TODO: Steam glitches the top sellers order so often that all the items just end up with the same sales eventually
            /*
            var item = orderedStoreItems.FirstOrDefault(x => x.Id == Id);
            var itemIndex = orderedStoreItems.IndexOf(item);
            var itemSales = Math.Max(TotalSalesMin, Description?.WorkshopFile?.Subscriptions ?? 0);
            var itemPrice = (item?.Price ?? 0);
            var itemRevenue = (itemPrice * itemSales);

            var beforeItemIndex = Math.Min((orderedStoreItems.IndexOf(item) + 1), orderedStoreItems.Count - 1);
            var beforeItem = (beforeItemIndex != itemIndex) ? orderedStoreItems.ElementAtOrDefault(beforeItemIndex) : null;
            var beforeItemSales = Math.Max(beforeItem?.TotalSalesMin ?? 0, beforeItem?.Description?.WorkshopFile?.Subscriptions ?? 0);
            var beforeItemPrice = (beforeItem?.Price ?? 0);
            var beforeItemRevenue = (beforeItemPrice * beforeItemSales);

            // If the item BELOW us in the top sellers has earned more revenue than us,
            // calculate min sales by inflating our subscriber count so that the revenue is at least equal
            var newTotalSalesMin = item.TotalSalesMin;
            if (beforeItemRevenue > itemRevenue && beforeItemRevenue > 0 && itemPrice > 0)
            {
                var minSubscribersToMeetRevenue = (int)(beforeItemRevenue / itemPrice);
                newTotalSalesMin = minSubscribersToMeetRevenue;
            }
            // Otherwise, we've earnt more revenue than the item below us, so just rely on subscriber count for minimum sales
            else
            {
                newTotalSalesMin = itemSales;
            }

            var afterItemIndex = Math.Max((orderedStoreItems.IndexOf(item) - 1), 0);
            var afterItem = (afterItemIndex != itemIndex) ? orderedStoreItems.ElementAtOrDefault(afterItemIndex) : null;
            var afterItemSales = Math.Max(afterItem?.TotalSalesMin ?? 0, afterItem?.Description?.WorkshopFile?.Subscriptions ?? 0);
            var afterItemPrice = (afterItem?.Price ?? 0);
            var afterItemRevenue = (afterItemPrice * afterItemSales);

            // If the item ABOVE us in the top sellers has earned more revenue than us,
            // calculate max sales by inflating our subscriber count so that the revenue is at least equal
            var newTotalSalesMax = item.TotalSalesMax;
            if (afterItemRevenue > itemRevenue && afterItemRevenue > 0 & itemPrice > 0)
            {
                var minSubscribersToMeetRevenue = (int)(afterItemRevenue / itemPrice);
                newTotalSalesMax = minSubscribersToMeetRevenue;
            }
            // Otherwise, we've earnt more revenue than the item below us, so just rely on subscriber count for minimum sales
            else
            {
                newTotalSalesMax = itemSales;
            }

            // Minimum sales should be the subscriber count if we are unsure
            TotalSalesMin = (beforeItem != null) ? newTotalSalesMin : itemSales;

            // Maximum sales should be null if we are unsure
            TotalSalesMax = (afterItem != null) ? newTotalSalesMax : null;
            */
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

            // Calculate the different between time in store vs. time in existance to see if there was any period where the item was not available
            var firstTimeSeen = limitedStores.Min(x => x.Store.Start.Value);
            var lastTimeSeen = limitedStores.Max(x => (x.Store.End ?? x.Store.Start.Value.AddDays(7)));
            var totalTimeInStore = limitedStores.Select(x => ((x.Store.End ?? x.Store.Start.Value.AddDays(7)) - x.Store.Start.Value)).Aggregate(TimeSpan.Zero, (t1, t2) => t1 + t2);
            var totalTimeInExistance = (lastTimeSeen - firstTimeSeen).Subtract(TimeSpan.FromHours(1));
            HasReturnedToStore = (totalTimeInExistance > totalTimeInStore);
        }
    }
}
