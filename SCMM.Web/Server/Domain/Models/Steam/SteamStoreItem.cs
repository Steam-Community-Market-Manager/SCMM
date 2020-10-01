using SCMM.Web.Server.Data.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public class SteamStoreItem : SteamItem
    {
        public SteamStoreItem()
        {
            StorePrices = new PersistablePriceDictionary();
            StoreRankGraph = new PersistableGraphDataSet();
            TotalSalesGraph = new PersistableGraphDataSet();
        }

        public PersistablePriceDictionary StorePrices { get; set; }

        public int StoreRankPosition { get; set; }

        public int StoreRankTotal { get; set; }

        public PersistableGraphDataSet StoreRankGraph { get; set; }

        public int TotalSalesMin { get; set; }

        public int? TotalSalesMax { get; set; }

        public PersistableGraphDataSet TotalSalesGraph { get; set; }

        public void RecalculateTotalSales(IEnumerable<SteamStoreItem> storeItems)
        {
            var orderedStoreItems = storeItems?.OrderBy(x => x.StoreRankPosition)?.ToList();
            if (orderedStoreItems == null)
            {
                return;
            }

            const string currency = "USD";

            var item = orderedStoreItems.FirstOrDefault(x => x.Id == Id);
            var itemIndex = orderedStoreItems.IndexOf(item);
            var itemSubscribers = (Description?.WorkshopFile?.Subscriptions ?? 0);
            var itemPrice = (item?.StorePrices[currency] ?? 0);
            var itemRevenue = (itemPrice * itemSubscribers);

            var beforeItemIndex = Math.Min((orderedStoreItems.IndexOf(item) + 1), orderedStoreItems.Count - 1);
            var beforeItem = (beforeItemIndex != itemIndex) ? orderedStoreItems.ElementAtOrDefault(beforeItemIndex) : null;
            var beforeItemSubscribers = (beforeItem?.Description?.WorkshopFile?.Subscriptions ?? 0);
            var beforeItemPrice = (beforeItem?.StorePrices[currency] ?? 0);
            var beforeItemRevenue = (beforeItemPrice * beforeItemSubscribers);

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
                newTotalSalesMin = itemSubscribers;
            }

            var afterItemIndex = Math.Max((orderedStoreItems.IndexOf(item) - 1), 0);
            var afterItem = (afterItemIndex != itemIndex) ? orderedStoreItems.ElementAtOrDefault(afterItemIndex) : null;
            var afterItemSubscribers = (afterItem?.Description?.WorkshopFile?.Subscriptions ?? 0);
            var afterItemPrice = (afterItem?.StorePrices[currency] ?? 0);
            var afterItemRevenue = (afterItemPrice * afterItemSubscribers);

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
                newTotalSalesMax = itemSubscribers;
            }

            // Minimum sales should be the subscriber count if we are unsure
            TotalSalesMin = (beforeItem != null) ? newTotalSalesMin : itemSubscribers;

            // Maximum sales should be null if we are unsure
            TotalSalesMax = (afterItem != null) ? newTotalSalesMax : null;
        }
    }
}
