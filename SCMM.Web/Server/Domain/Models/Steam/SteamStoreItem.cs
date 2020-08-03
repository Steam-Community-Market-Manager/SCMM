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

            var me = orderedStoreItems.FirstOrDefault(x => x.Id == Id);
            var meIndex = orderedStoreItems.IndexOf(me);
            var beforeMeIndex = Math.Min((orderedStoreItems.IndexOf(me) + 1), orderedStoreItems.Count - 1);
            var beforeMeItem = (beforeMeIndex != meIndex) ? orderedStoreItems.ElementAtOrDefault(beforeMeIndex) : null;
            var afterMeIndex = Math.Max((orderedStoreItems.IndexOf(me) - 1), 0);
            var afterMeItem = (afterMeIndex != meIndex) ? orderedStoreItems.ElementAtOrDefault(afterMeIndex) : null;
            var totalSubscribers = (Description?.WorkshopFile?.Subscriptions ?? 0);

            var newTotalSalesMin = (beforeMeItem != null)
                ? Math.Max(beforeMeItem.TotalSalesMin + 1, totalSubscribers)
                : totalSubscribers; // bottom of the list
            var newTotalSalesMax = (afterMeItem != null)
                ? (int?)Math.Max(afterMeItem.TotalSalesMin - 1, TotalSalesMin)
                : Int32.MaxValue; // top of the list

            // Minimum sales should never drop below its current value. If another item overtakes us in sales, 
            // we need to remember we still sold alot rather than falling back to subscriber count (which isn't an accurate representation of sales)
            TotalSalesMin = Math.Max(TotalSalesMin, newTotalSalesMin);

            // Maximum sales should be null if we have no idea how many could have been sold.
            TotalSalesMax = (TotalSalesMin != newTotalSalesMax) ? newTotalSalesMax : null;
        }
    }
}
