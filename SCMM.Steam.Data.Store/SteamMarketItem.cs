using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store.Types;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Steam.Data.Store
{
    public class SteamMarketItem : SteamItem
    {
        public SteamMarketItem()
        {
            Activity = new Collection<SteamMarketItemActivity>();
            BuyOrders = new Collection<SteamMarketItemBuyOrder>();
            BuyOrderHighestPriceRolling24hrs = new PersistablePriceCollection();
            SellOrders = new Collection<SteamMarketItemSellOrder>();
            SellOrderLowestPriceRolling24hrs = new PersistablePriceCollection();
            OrdersHistory = new Collection<SteamMarketItemOrderSummary>();
            SalesHistory = new Collection<SteamMarketItemSale>();
            SalesPriceRolling24hrs = new PersistablePriceCollection();
        }

        public Guid? CurrencyId { get; set; }

        public SteamCurrency Currency { get; set; }

        public ICollection<SteamMarketItemActivity> Activity { get; set; }

        public ICollection<SteamMarketItemBuyOrder> BuyOrders { get; set; }

        // What is the total quantity of all buy orders
        public int BuyOrderCount { get; set; }

        // What is the total price of all buy orders added together
        public long BuyOrderCumulativePrice { get; set; }

        // What is the most expensive buy order
        public long BuyOrderHighestPrice { get; set; }

        // What was the most expensive buy order starting at todays open (UTC)
        public long Stable24hrBuyOrderHighestPrice { get; set; }

        [Required]
        public PersistablePriceCollection BuyOrderHighestPriceRolling24hrs { get; set; }

        public ICollection<SteamMarketItemSellOrder> SellOrders { get; set; }

        // What is the total quantity of all sell orders
        public int SellOrderCount { get; set; }

        // What is the total price of all sell orders added together
        public long SellOrderCumulativePrice { get; set; }

        // What is the cheapest sell order
        public long SellOrderLowestPrice { get; set; }

        // What was the cheapest sell order starting at todays open (UTC)
        public long Stable24hrSellOrderLowestPrice { get; set; }

        [Required]
        public PersistablePriceCollection SellOrderLowestPriceRolling24hrs { get; set; }

        // What is the price you could reasonably flip this for given the current sell orders
        public long ResellPrice { get; set; }

        // What tax is owed on resell price
        public long ResellTax { get; set; }

        public ICollection<SteamMarketItemOrderSummary> OrdersHistory { get; set; }

        public ICollection<SteamMarketItemSale> SalesHistory { get; set; }

        [Required]
        public PersistablePriceCollection SalesPriceRolling24hrs { get; set; }

        // What was the total number of sales from the first 24hrs (1 day)
        public long First24hrSales { get; set; }

        // What was the average price from the first 24hrs (1 day)
        public long First24hrValue { get; set; }

        // What was the total number of sales from the last hour 
        public long Last1hrSales { get; set; }

        // What was the average price from the last hour
        public long Last1hrValue { get; set; }

        // What was the total number of sales from the last 24hrs (1 day)
        public long Last24hrSales { get; set; }

        // What was the average price from the last 24hrs (1 day)
        public long Last24hrValue { get; set; }

        // What was the price starting at todays open (UTC) or the last time it was sold (if no sales in last 24hrs)
        public long Stable24hrValue { get; set; }

        // What was the total number of sales from the last 48hrs (2 days)
        public long Last48hrSales { get; set; }

        // What was the average price from the last 48hrs (2 days)
        public long Last48hrValue { get; set; }

        // What was the total number of sales from the last 72hrs (3 days)
        public long Last72hrSales { get; set; }

        // What was the average price from the last 72hrs (3 days)
        public long Last72hrValue { get; set; }

        // What was the total number of sales from the last 96hrs (4 days)
        public long Last96hrSales { get; set; }

        // What was the average price from the last 96hrs (4 days)
        public long Last96hrValue { get; set; }

        // What was the total number of sales from the last 48hrs (5 days)
        public long Last120hrSales { get; set; }

        // What was the average price from the last 120hrs (5 days)
        public long Last120hrValue { get; set; }

        // What was the total number of sales from the last 144hrs (6 days)
        public long Last144hrSales { get; set; }

        // What was the average price from the last 144hrs (6 days)
        public long Last144hrValue { get; set; }

        // What was the total number of sales from the last 168hrs (7 days)
        public long Last168hrSales { get; set; }

        // What was the average price from the last 168hrs (7 days)
        public long Last168hrValue { get; set; }

        // What was the price from the last sale (at any time range)
        public long LastSaleValue { get; set; }

        public DateTimeOffset? LastSaleOn { get; set; }

        // What was the all-time average price this sells for
        public long AllTimeAverageValue { get; set; }

        // What was the all-time highest price this ever sold for
        public long AllTimeHighestValue { get; set; }

        public DateTimeOffset? AllTimeHighestValueOn { get; set; }

        // What was the all-time lowest price this ever sold for
        public long AllTimeLowestValue { get; set; }

        public DateTimeOffset? AllTimeLowestValueOn { get; set; }

        // When was the very first sale
        public DateTimeOffset? FirstSaleOn { get; set; }

        // How long since orders were last checked
        public DateTimeOffset? LastCheckedOrdersOn { get; set; }

        // How long since prices were last checked
        public DateTimeOffset? LastCheckedSalesOn { get; set; }

        public void RecalculateOrders(SteamMarketItemBuyOrder[] buyOrders = null, int? buyOrderCount = null, SteamMarketItemSellOrder[] sellOrders = null, int? sellOrderCount = null)
        {
            var now = DateTimeOffset.UtcNow;
            var dayOpenTimestamp = new DateTimeOffset(DateTime.UtcNow.Date, TimeZoneInfo.Utc.BaseUtcOffset);

            // Recalculate buy order stats
            if (buyOrders != null)
            {
                // Add new orders, remove old orders, update existing orders
                BuyOrders.AddRange(buyOrders.Where(x => !BuyOrders.Any(y => x.Price == y.Price)));
                BuyOrders.RemoveAll(x => !buyOrders.Any(y => x.Price == y.Price));
                foreach (var order in BuyOrders.Join(buyOrders, x => x.Price, x => x.Price, (x, y) => new { Old = x, New = y }))
                {
                    order.Old.Quantity = order.New.Quantity;
                }

                var buyOrdersSorted = BuyOrders.OrderByDescending(y => y.Price).ToArray();
                var cumulativeBuyOrderPrice = (buyOrdersSorted.Any() ? buyOrdersSorted.Sum(x => x.Price * x.Quantity) : 0);
                var highestBuyOrderPrice = (buyOrdersSorted.Any() ? buyOrdersSorted.Max(x => x.Price) : 0);
                
                // NOTE: Steam only returns the top 100 orders, so the true count can't be calculated from sell orders list
                //BuyOrderCount = buyOrdersSorted.Sum(y => y.Quantity);
                BuyOrderCount = (buyOrderCount ?? BuyOrderCount);
                BuyOrderCumulativePrice = cumulativeBuyOrderPrice;
                BuyOrderHighestPrice = highestBuyOrderPrice;
            }

            // Recalculate sell order stats
            if (sellOrders != null)
            {
                // Add new orders, remove old orders, update existing orders
                SellOrders.AddRange(sellOrders.Where(x => !SellOrders.Any(y => x.Price == y.Price)));
                SellOrders.RemoveAll(x => !sellOrders.Any(y => x.Price == y.Price));
                foreach (var order in SellOrders.Join(sellOrders, x => x.Price, x => x.Price, (x, y) => new { Old = x, New = y }))
                {
                    order.Old.Quantity = order.New.Quantity;
                }

                var sellOrdersSorted = SellOrders.OrderBy(y => y.Price).ToArray();
                var cumulativeSellOrderPrice = (sellOrdersSorted.Any() ? sellOrdersSorted.Sum(x => x.Price * x.Quantity) : 0);
                var lowestSellOrderPrice = (sellOrdersSorted.Any() ? sellOrdersSorted.Min(x => x.Price) : 0);
                var resellPrice = (lowestSellOrderPrice - 1);
                var resellTax = resellPrice.SteamFeeAsInt();

                // NOTE: Steam only returns the top 100 orders, so the true count can't be calculated from sell orders list
                //SellOrderCount = sellOrdersSorted.Sum(y => y.Quantity);
                SellOrderCount = (sellOrderCount ?? SellOrderCount);
                SellOrderCumulativePrice = cumulativeSellOrderPrice;
                SellOrderLowestPrice = lowestSellOrderPrice;
                ResellPrice = resellPrice;
                ResellTax = resellTax;
            }
            /*
            // Update the latest order summary for the hour
            var hourOpenTimestamp = new DateTimeOffset(DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour), TimeZoneInfo.Utc.BaseUtcOffset);
            var latestOrderSummary = OrdersHistory.FirstOrDefault(x => x.Timestamp == hourOpenTimestamp);
            if (latestOrderSummary == null)
            {
                OrdersHistory.Add(latestOrderSummary = new SteamMarketItemOrderSummary()
                {
                    Timestamp = hourOpenTimestamp
                });
            }
            if (latestOrderSummary != null)
            {
                latestOrderSummary.BuyCount = BuyOrderCount;
                latestOrderSummary.BuyCumulativePrice = BuyOrderCumulativePrice;
                latestOrderSummary.BuyHighestPrice = BuyOrderHighestPrice;
                latestOrderSummary.SellCount = SellOrderCount;
                latestOrderSummary.SellCumulativePrice = SellOrderCumulativePrice;
                latestOrderSummary.SellLowestPrice = SellOrderLowestPrice;
            }

            // Update the rolling 24hr values
            var orderHistorySorted = OrdersHistory.OrderByDescending(x => x.Timestamp);
            var buyOrderHighestPriceRolling24hrs = new List<long>();
            var sellOrderLowestPriceRolling24hrs = new List<long>();
            for (int i = 0; i < 24; i++)
            {
                var summary = orderHistorySorted.FirstOrDefault(x => x.Timestamp == hourOpenTimestamp.Subtract(TimeSpan.FromHours(i)));
                buyOrderHighestPriceRolling24hrs.Add(summary?.BuyHighestPrice ?? buyOrderHighestPriceRolling24hrs.LastOrDefault());
                sellOrderLowestPriceRolling24hrs.Add(summary?.SellLowestPrice ?? sellOrderLowestPriceRolling24hrs.LastOrDefault());
            }
            BuyOrderHighestPriceRolling24hrs = new PersistablePriceCollection(buyOrderHighestPriceRolling24hrs);
            SellOrderLowestPriceRolling24hrs = new PersistablePriceCollection(sellOrderLowestPriceRolling24hrs);
            */
        }

        public void RecalculateSales(SteamMarketItemSale[] newSales = null)
        {
            // Add any new sales we don't already have
            if (newSales != null)
            {
                SalesHistory.AddRange(newSales.Where(x => !SalesHistory.Any(y => x.Timestamp == y.Timestamp)));
            }

            // Sort sales from earliest to latest
            var salesSorted = SalesHistory.OrderBy(y => y.Timestamp).ToArray();
            if (!salesSorted.Any())
            {
                return;
            }

            // Recalculate sales stats
            var now = DateTimeOffset.UtcNow;
            var earliestTimestamp = salesSorted.Min(x => x.Timestamp);
            var latestTimestamp = salesSorted.Max(x => x.Timestamp);
            var firstSaleOn = salesSorted.FirstOrDefault()?.Timestamp;
            var first24hrs = salesSorted.Where(x => x.Timestamp <= earliestTimestamp.Add(TimeSpan.FromHours(24))).ToArray();
            var first24hrSales = first24hrs.Sum(x => x.Quantity);
            var first24hrValue = (long)Math.Round(first24hrs.Length > 0 ? first24hrs.Average(x => x.MedianPrice) : 0, 0);
            var last1hrs = salesSorted.Where(x => x.Timestamp >= now.Subtract(TimeSpan.FromHours(1))).ToArray();
            var last1hrSales = last1hrs.Sum(x => x.Quantity);
            var last1hrValue = (long)Math.Round(last1hrs.Length > 0 ? last1hrs.Average(x => x.MedianPrice) : 0, 0);
            var last24hrs = salesSorted.Where(x => x.Timestamp >= now.Subtract(TimeSpan.FromHours(24))).ToArray();
            var last24hrSales = last24hrs.Sum(x => x.Quantity);
            var last24hrValue = (long)Math.Round(last24hrs.Length > 0 ? last24hrs.Average(x => x.MedianPrice) : 0, 0);
            var last48hrs = salesSorted.Where(x => x.Timestamp >= now.Subtract(TimeSpan.FromHours(48))).ToArray();
            var last48hrSales = last48hrs.Sum(x => x.Quantity);
            var last48hrValue = (long)Math.Round(last48hrs.Length > 0 ? last48hrs.Average(x => x.MedianPrice) : 0, 0);
            var last72hrs = salesSorted.Where(x => x.Timestamp >= now.Subtract(TimeSpan.FromHours(72))).ToArray();
            var last72hrSales = last72hrs.Sum(x => x.Quantity);
            var last72hrValue = (long)Math.Round(last72hrs.Length > 0 ? last72hrs.Average(x => x.MedianPrice) : 0, 0);
            var last96hrs = salesSorted.Where(x => x.Timestamp >= now.Subtract(TimeSpan.FromHours(96))).ToArray();
            var last96hrSales = last96hrs.Sum(x => x.Quantity);
            var last96hrValue = (long)Math.Round(last96hrs.Length > 0 ? last96hrs.Average(x => x.MedianPrice) : 0, 0);
            var last120hrs = salesSorted.Where(x => x.Timestamp >= now.Subtract(TimeSpan.FromHours(120))).ToArray();
            var last120hrSales = last120hrs.Sum(x => x.Quantity);
            var last120hrValue = (long)Math.Round(last120hrs.Length > 0 ? last120hrs.Average(x => x.MedianPrice) : 0, 0);
            var last144hrs = salesSorted.Where(x => x.Timestamp >= now.Subtract(TimeSpan.FromHours(144))).ToArray();
            var last144hrSales = last144hrs.Sum(x => x.Quantity);
            var last144hrValue = (long)Math.Round(last144hrs.Length > 0 ? last144hrs.Average(x => x.MedianPrice) : 0, 0);
            var last168hrs = salesSorted.Where(x => x.Timestamp >= now.Subtract(TimeSpan.FromHours(168))).ToArray();
            var last168hrSales = last168hrs.Sum(x => x.Quantity);
            var last168hrValue = (long)Math.Round(last168hrs.Length > 0 ? last168hrs.Average(x => x.MedianPrice) : 0, 0);
            var lastSaleValue = (salesSorted.LastOrDefault()?.MedianPrice ?? 0);
            var lastSaleOn = salesSorted.LastOrDefault()?.Timestamp;

            FirstSaleOn = firstSaleOn;
            First24hrSales = first24hrSales;
            First24hrValue = first24hrValue;
            Last1hrSales = last1hrSales;
            Last1hrValue = last1hrValue;
            Last24hrSales = last24hrSales;
            Last24hrValue = last24hrValue;
            Last48hrSales = last48hrSales;
            Last48hrValue = last48hrValue;
            Last72hrSales = last72hrSales;
            Last72hrValue = last72hrValue;
            Last96hrSales = last96hrSales;
            Last96hrValue = last96hrValue;
            Last120hrSales = last48hrSales;
            Last120hrValue = last120hrValue;
            Last144hrSales = last144hrSales;
            Last144hrValue = last144hrValue;
            Last168hrSales = last168hrSales;
            Last168hrValue = last168hrValue;
            LastSaleValue = lastSaleValue;
            LastSaleOn = lastSaleOn;

            // The first three days on the market is always overinflated, filter these out before calculating overall averages
            var salesAfterFirstSevenDays = salesSorted.Where(x => x.Timestamp >= earliestTimestamp.AddDays(7)).ToArray();
            if (salesAfterFirstSevenDays.Any())
            {
                var allTimeAverage = (long)Math.Round(salesAfterFirstSevenDays.Length > 0 ? salesAfterFirstSevenDays.Average(x => x.MedianPrice) : 0, 0);
                var allTimeLow = salesAfterFirstSevenDays.FirstOrDefault(x => x.MedianPrice == salesAfterFirstSevenDays.Min(x => x.MedianPrice));
                var allTimeHigh = salesAfterFirstSevenDays.FirstOrDefault(x => x.MedianPrice == salesAfterFirstSevenDays.Max(x => x.MedianPrice));
                AllTimeAverageValue = allTimeAverage;
                AllTimeHighestValue = (allTimeHigh?.MedianPrice ?? 0);
                AllTimeHighestValueOn = allTimeHigh?.Timestamp;
                AllTimeLowestValue = (allTimeLow?.MedianPrice ?? 0);
                AllTimeLowestValueOn = allTimeLow?.Timestamp;
            }
        }
    }
}
