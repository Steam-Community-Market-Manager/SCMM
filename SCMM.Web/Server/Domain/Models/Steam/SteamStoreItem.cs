using SCMM.Web.Server.Data.Types;
using System;
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

        public void RecalculateTotalSales(SteamStoreItem[] storeItems)
        {
            var orderedStoreItems = storeItems?.OrderBy(x => x.StoreRankPosition)?.ToList();
            if (orderedStoreItems == null)
            {
                return;
            }

            var item = orderedStoreItems.FirstOrDefault(x => x.Id == Id);
            var itemIndex = orderedStoreItems.IndexOf(item);
            var itemSubscribers = (Description?.WorkshopFile?.Subscriptions ?? 0);
            var beforeItemIndex = Math.Min((orderedStoreItems.IndexOf(item) + 1), orderedStoreItems.Count - 1);
            var beforeItem = (beforeItemIndex != itemIndex) ? orderedStoreItems.ElementAtOrDefault(beforeItemIndex) : null;
            var afterItemIndex = Math.Max((orderedStoreItems.IndexOf(item) - 1), 0);
            var afterItem = (afterItemIndex != itemIndex) ? orderedStoreItems.ElementAtOrDefault(afterItemIndex) : null;

            // TODO: Redo this based on total revenue, not subscriber counts
            TotalSalesMin = itemSubscribers;
            /*
            var newTotalSalesMin = (beforeItem != null)
                ? Math.Max(beforeItem.TotalSalesMin + 1, itemSubscribers)
                : itemSubscribers; // bottom of the list
            var newTotalSalesMax = (afterItem != null)
                ? (int?)Math.Max(afterItem.TotalSalesMin - 1, TotalSalesMin)
                : Int32.MaxValue; // top of the list

            // Minimum sales should never drop below its current value. If another item overtakes us in sales, 
            // we need to remember we still sold alot rather than falling back to subscriber count (which isn't an accurate representation of sales)
            TotalSalesMin = Math.Max(TotalSalesMin, newTotalSalesMin);

            // Maximum sales should be null if we have no idea how many could have been sold.
            TotalSalesMax = (TotalSalesMin != newTotalSalesMax) ? newTotalSalesMax : null;
            */
        }
    }
}
