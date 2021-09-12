using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models.Extensions;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace SCMM.Steam.Data.Store
{
    public class SteamMarketItem : SteamItem
    {
        public SteamMarketItem()
        {
            BuyOrders = new Collection<SteamMarketItemBuyOrder>();
            SellOrders = new Collection<SteamMarketItemSellOrder>();
            SalesHistory = new Collection<SteamMarketItemSale>();
            Activity = new Collection<SteamMarketItemActivity>();
        }

        public Guid? CurrencyId { get; set; }

        public SteamCurrency Currency { get; set; }

        public ICollection<SteamMarketItemBuyOrder> BuyOrders { get; set; }

        public ICollection<SteamMarketItemSellOrder> SellOrders { get; set; }

        public ICollection<SteamMarketItemSale> SalesHistory { get; set; }

        public ICollection<SteamMarketItemActivity> Activity { get; set; }

        // What is the total quantity of all sell orders
        public int Supply { get; set; }

        // What is the total quantity of all buy orders
        public int Demand { get; set; }

        // What is the cheapest buy order
        public long BuyAskingPrice { get; set; }

        // What is the cheapest sell order
        public long BuyNowPrice { get; set; }

        // What is the price you could reasonably flip this for given the current buy orders
        public long ResellPrice { get; set; }

        // What tax is owed on resell price
        public long ResellTax { get; set; }

        // What is the difference between buy now and resell prices
        public long ResellProfit { get; set; }

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

        // Was was the price starting at todays open (UTC)
        public long Open24hrValue { get; set; }

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
        public long? LastSaleValue { get; set; }

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
        public DateTimeOffset? FirstSeenOn { get; set; }

        // How long since orders were last checked
        public DateTimeOffset? LastCheckedOrdersOn { get; set; }

        // How long since prices were last checked
        public DateTimeOffset? LastCheckedSalesOn { get; set; }

        public void RecalculateOrders(SteamMarketItemBuyOrder[] buyOrders = null, int? buyOrderCount = null, SteamMarketItemSellOrder[] sellOrders = null, int? sellOrderCount = null)
        {
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
                var highestPrice = (buyOrdersSorted.Length > 0)
                    ? buyOrdersSorted.First().Price
                    : 0;

                // NOTE: Steam only returns the top 100 orders, so the true demand can't be calculated from sell orders list
                //Demand = buyOrdersSorted.Sum(y => y.Quantity);
                Demand = (buyOrderCount ?? Demand);
                BuyAskingPrice = highestPrice;
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
                var lowestBuyNowPrice = (sellOrdersSorted.Length > 0)
                    ? sellOrdersSorted.First().Price
                    : 0;
                var secondLowestBuyNowPrice = (sellOrdersSorted.Length > 1)
                    ? sellOrdersSorted.Skip(1).First().Price
                    : lowestBuyNowPrice;
                var averageBuyNowPrice = (sellOrdersSorted.Length > 1)
                    ? (long)Math.Ceiling((decimal)sellOrdersSorted.Skip(1).Sum(y => y.Price) / (sellOrdersSorted.Length - 1))
                    : 0;
                var resellPrice = secondLowestBuyNowPrice;
                var resellTax = resellPrice.SteamFeeAsInt();

                // NOTE: Steam only returns the top 100 orders, so the true supply can't be calculated from sell orders list
                //Supply = sellOrdersSorted.Sum(y => y.Quantity);
                Supply = (sellOrderCount ?? Supply);
                BuyNowPrice = lowestBuyNowPrice;
                ResellPrice = resellPrice;
                ResellTax = resellTax;
                ResellProfit = (resellPrice - resellTax - lowestBuyNowPrice);
            }
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
            var dayOpenTimestamp = new DateTimeOffset(DateTime.UtcNow.Date, TimeZoneInfo.Utc.BaseUtcOffset);
            var earliestTimestamp = salesSorted.Min(x => x.Timestamp);
            var latestTimestamp = salesSorted.Max(x => x.Timestamp);
            var firstSaleOn = salesSorted.FirstOrDefault()?.Timestamp;
            var first24hrs = salesSorted.Where(x => x.Timestamp <= earliestTimestamp.Add(TimeSpan.FromHours(24))).ToArray();
            var first24hrSales = first24hrs.Sum(x => x.Quantity);
            var first24hrValue = (long)Math.Round(first24hrs.Length > 0 ? first24hrs.Average(x => x.Price) : 0, 0);
            var stable24hrs = salesSorted.Where(x => x.Timestamp >= dayOpenTimestamp.AddDays(-1) && x.Timestamp <= dayOpenTimestamp).ToArray();
            var stable24hrAverageValue = (long)Math.Round(stable24hrs.Length > 0 ? stable24hrs.Average(x => x.Price) : 0, 0);
            var last1hrs = salesSorted.Where(x => x.Timestamp >= now.Subtract(TimeSpan.FromHours(1))).ToArray();
            var last1hrSales = last1hrs.Sum(x => x.Quantity);
            var last1hrValue = (long)Math.Round(last1hrs.Length > 0 ? last1hrs.Average(x => x.Price) : 0, 0);
            var last24hrs = salesSorted.Where(x => x.Timestamp >= now.Subtract(TimeSpan.FromHours(24))).ToArray();
            var last24hrSales = last24hrs.Sum(x => x.Quantity);
            var last24hrValue = (long)Math.Round(last24hrs.Length > 0 ? last24hrs.Average(x => x.Price) : 0, 0);
            var last48hrs = salesSorted.Where(x => x.Timestamp >= now.Subtract(TimeSpan.FromHours(48))).ToArray();
            var last48hrSales = last48hrs.Sum(x => x.Quantity);
            var last48hrValue = (long)Math.Round(last48hrs.Length > 0 ? last48hrs.Average(x => x.Price) : 0, 0);
            var last72hrs = salesSorted.Where(x => x.Timestamp >= now.Subtract(TimeSpan.FromHours(72))).ToArray();
            var last72hrSales = last72hrs.Sum(x => x.Quantity);
            var last72hrValue = (long)Math.Round(last72hrs.Length > 0 ? last72hrs.Average(x => x.Price) : 0, 0);
            var last96hrs = salesSorted.Where(x => x.Timestamp >= now.Subtract(TimeSpan.FromHours(96))).ToArray();
            var last96hrSales = last96hrs.Sum(x => x.Quantity);
            var last96hrValue = (long)Math.Round(last96hrs.Length > 0 ? last96hrs.Average(x => x.Price) : 0, 0);
            var last120hrs = salesSorted.Where(x => x.Timestamp >= now.Subtract(TimeSpan.FromHours(120))).ToArray();
            var last120hrSales = last120hrs.Sum(x => x.Quantity);
            var last120hrValue = (long)Math.Round(last120hrs.Length > 0 ? last120hrs.Average(x => x.Price) : 0, 0);
            var last144hrs = salesSorted.Where(x => x.Timestamp >= now.Subtract(TimeSpan.FromHours(144))).ToArray();
            var last144hrSales = last144hrs.Sum(x => x.Quantity);
            var last144hrValue = (long)Math.Round(last144hrs.Length > 0 ? last144hrs.Average(x => x.Price) : 0, 0);
            var last168hrs = salesSorted.Where(x => x.Timestamp >= now.Subtract(TimeSpan.FromHours(168))).ToArray();
            var last168hrSales = last168hrs.Sum(x => x.Quantity);
            var last168hrValue = (long)Math.Round(last168hrs.Length > 0 ? last168hrs.Average(x => x.Price) : 0, 0);
            var lastSaleValue = (salesSorted.LastOrDefault()?.Price ?? 0);
            var lastSaleOn = salesSorted.LastOrDefault()?.Timestamp;

            FirstSeenOn = firstSaleOn;
            First24hrSales = first24hrSales;
            First24hrValue = first24hrValue;
            Last1hrSales = last1hrSales;
            Last1hrValue = last1hrValue;
            Open24hrValue = (stable24hrAverageValue > 0 ? stable24hrAverageValue : lastSaleValue);
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
                var allTimeAverage = (long)Math.Round(salesAfterFirstSevenDays.Length > 0 ? salesAfterFirstSevenDays.Average(x => x.Price) : 0, 0);
                var allTimeLow = salesAfterFirstSevenDays.FirstOrDefault(x => x.Price == salesAfterFirstSevenDays.Min(x => x.Price));
                var allTimeHigh = salesAfterFirstSevenDays.FirstOrDefault(x => x.Price == salesAfterFirstSevenDays.Max(x => x.Price));
                AllTimeAverageValue = allTimeAverage;
                AllTimeHighestValue = (allTimeHigh?.Price ?? 0);
                AllTimeHighestValueOn = allTimeHigh?.Timestamp;
                AllTimeLowestValue = (allTimeLow?.Price ?? 0);
                AllTimeLowestValueOn = allTimeLow?.Timestamp;
            }
        }
    }
}
