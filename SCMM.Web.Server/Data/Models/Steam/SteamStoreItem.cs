﻿using SCMM.Web.Data.Models.Steam;
using SCMM.Web.Server.Data.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SCMM.Web.Server.Data.Models.Steam
{
    public class SteamStoreItem : SteamItem
    {
        public SteamStoreItem()
        {
            Stores = new Collection<SteamStoreItemItemStore>();
            Prices = new PersistablePriceDictionary();
            TotalSalesGraph = new PersistableDailyGraphDataSet();
        }

        public Guid? CurrencyId { get; set; }

        public SteamCurrency Currency { get; set; }

        public long Price { get; set; }

        /// <summary>
        /// Store prices are generally fixed and don't fluxuate with currency exhange rates.
        /// Because of this, we need to keep a list of all the fixed store prices in each currency.
        /// </summary>
        public PersistablePriceDictionary Prices { get; set; }

        public ICollection<SteamStoreItemItemStore> Stores { get; set; }

        public int TotalSalesMin { get; set; }

        public int? TotalSalesMax { get; set; }

        public PersistableDailyGraphDataSet TotalSalesGraph { get; set; }

        public SteamStoreItemFlags Flags { get; set; }

        public void RecalculateTotalSales(SteamItemStore store)
        {
            var mapping = Stores.FirstOrDefault(x => x.Store == store);
            var orderedStoreItems = mapping?.Store?.Items?.OrderBy(x => x.Index)?.Select(x => x.Item)?.ToList();
            if (orderedStoreItems == null)
            {
                return;
            }

            // NOTE: This approach just assumes a 10-20% increase over subscriptions
            var item = orderedStoreItems.FirstOrDefault(x => x.Id == Id);
            var itemIndex = orderedStoreItems.IndexOf(item);
            var itemUniqueSales = (Description?.WorkshopFile?.Subscriptions ?? 0);
            var itemDuplicateSales = (int)Math.Floor(itemUniqueSales > 0 ? ((decimal)itemUniqueSales / Math.Max(10, 20 - itemIndex)) : 0);
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
    }
}