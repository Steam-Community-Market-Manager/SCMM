using SCMM.Steam.Data.Models.Extensions;
using System;
using System.Collections.Generic;
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

        // What is the difference between the cheapest and 2nd cheapest sell orders
        public long BuyNowPriceDelta { get; set; }

        // What is the price you could reasonably flip this for given the current buy orders
        public long ResellPrice { get; set; }

        // What tax is owed on resell price
        public long ResellTax { get; set; }

        // What is the difference between buy now and resell prices
        public long ResellProfit { get; set; }

        [NotMapped]
        public bool WouldResellProfit => (ResellProfit >= 0);

        [NotMapped]
        public bool WouldResellLoss => (ResellProfit < 0);

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

        // What is the difference between current and 120hr sale prices
        [NotMapped]
        public long MovementLast24hrValue => (Last1hrValue - Last24hrValue);

        // What is the difference between current and 48hr sale prices
        [NotMapped]
        public long MovementLast48hrValue => (Last1hrValue - Last48hrValue);

        // What is the difference between current and 72hr sale prices
        [NotMapped]
        public long MovementLast72hrValue => (Last1hrValue - Last72hrValue);

        // What is the difference between current and 96hr sale prices
        [NotMapped]
        public long MovementLast96hrValue => (Last1hrValue - Last96hrValue);

        // What is the difference between current and 120hr sale prices
        [NotMapped]
        public long MovementLast120hrValue => (Last1hrValue - Last120hrValue);

        // What is the difference between current and 144hr sale prices
        [NotMapped]
        public long MovementLast144hrValue => (Last1hrValue - Last144hrValue);

        // What is the difference between current and 168hr sale prices
        [NotMapped]
        public long MovementLast168hrValue => (Last1hrValue - Last168hrValue);

        // What is the difference between current and original sale prices
        [NotMapped]
        public long MovementAllTimeValue => (Last1hrValue - First24hrValue);

        [NotMapped]
        public bool HasAppreciated => (Last1hrValue >= First24hrValue);

        [NotMapped]
        public bool HasDepreciated => (Last1hrValue < First24hrValue);

        // What was the all-time average price this sells for
        public long AllTimeAverageValue { get; set; }

        // What was the all-time highest price this ever sold for
        public long AllTimeHighestValue { get; set; }

        public DateTimeOffset? AllTimeHighestValueOn { get; set; }

        // What was the all-time lowest price this ever sold for
        public long AllTimeLowestValue { get; set; }

        public DateTimeOffset? AllTimeLowestValueOn { get; set; }

        // What is the difference between all-time highest and lowest sale prices
        public long AllTimeSwingValue => (AllTimeHighestValue - AllTimeLowestValue);

        // When was the very first sale
        public DateTimeOffset? FirstSeenOn { get; set; }

        public TimeSpan? MarketAge => (DateTimeOffset.Now - FirstSeenOn);

        // How long since orders were last checked
        public DateTimeOffset? LastCheckedOrdersOn { get; set; }

        // How long since prices were last checked
        public DateTimeOffset? LastCheckedSalesOn { get; set; }

        public void RecalculateOrders(SteamMarketItemBuyOrder[] buyOrders = null, int? buyOrderCount = null, SteamMarketItemSellOrder[] sellOrders = null, int? sellOrderCount = null)
        {
            var buyOrdersSafe = (buyOrders ?? BuyOrders?.ToArray());
            if (buyOrdersSafe != null)
            {
                var buyOrdersSorted = buyOrdersSafe.OrderByDescending(y => y.Price).ToArray();
                var highestPrice = (buyOrdersSorted.Length > 0)
                    ? buyOrdersSorted.First().Price
                    : 0;

                // NOTE: Steam only returns the top 100 orders, so the true demand can't be calculated from sell orders list
                //Demand = buyOrdersSorted.Sum(y => y.Quantity);
                Demand = (buyOrderCount ?? Demand);

                BuyAskingPrice = highestPrice;
                if (buyOrders != null)
                {
                    BuyOrders.Clear();
                    foreach (var order in buyOrdersSorted)
                    {
                        BuyOrders.Add(order);
                    }
                }
            }

            var sellOrdersSafe = (sellOrders ?? SellOrders?.ToArray());
            if (sellOrdersSafe != null)
            {
                var sellOrdersSorted = sellOrdersSafe.OrderBy(y => y.Price).ToArray();
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
                BuyNowPriceDelta = (secondLowestBuyNowPrice - lowestBuyNowPrice);
                ResellPrice = resellPrice;
                ResellTax = resellTax;
                ResellProfit = (resellPrice - resellTax - lowestBuyNowPrice);
                if (sellOrders != null)
                {
                    SellOrders.Clear();
                    foreach (var order in sellOrdersSorted)
                    {
                        SellOrders.Add(order);
                    }
                }
            }
        }

        public void RecalculateSales(SteamMarketItemSale[] sales = null)
        {
            var salesSafe = (sales ?? SalesHistory?.ToArray());
            if (salesSafe == null)
            {
                return;
            }

            var salesSorted = salesSafe.OrderBy(y => y.Timestamp).ToArray();
            if (!salesSorted.Any())
            {
                return;
            }

            var earliestTimestamp = salesSorted.Min(x => x.Timestamp);
            var latestTimestamp = salesSorted.Max(x => x.Timestamp);

            var first24hrs = salesSorted.Where(x => x.Timestamp <= earliestTimestamp.Add(TimeSpan.FromHours(24)) && x.Timestamp > earliestTimestamp).ToArray();
            var first24hrValue = (long)Math.Round(first24hrs.Length > 0 ? first24hrs.Average(x => x.Price) : 0, 0);

            var last1hrs = salesSorted.Where(x => x.Timestamp == latestTimestamp).ToArray();
            var last1hrSales = last1hrs.Sum(x => x.Quantity);
            var last1hrValue = (long)Math.Round(last1hrs.Length > 0 ? last1hrs.Average(x => x.Price) : 0, 0);
            var last24hrs = salesSorted.Where(x => x.Timestamp >= latestTimestamp.Subtract(TimeSpan.FromHours(24)) && x.Timestamp < latestTimestamp).ToArray();
            var last24hrSales = last24hrs.Sum(x => x.Quantity);
            var last24hrValue = (long)Math.Round(last24hrs.Length > 0 ? last24hrs.Average(x => x.Price) : 0, 0);
            var last48hrs = salesSorted.Where(x => x.Timestamp >= latestTimestamp.Subtract(TimeSpan.FromHours(48)) && x.Timestamp < latestTimestamp.Subtract(TimeSpan.FromHours(24))).ToArray();
            var last48hrSales = last48hrs.Sum(x => x.Quantity);
            var last48hrValue = (long)Math.Round(last48hrs.Length > 0 ? last48hrs.Average(x => x.Price) : 0, 0);
            var last72hrs = salesSorted.Where(x => x.Timestamp >= latestTimestamp.Subtract(TimeSpan.FromHours(72)) && x.Timestamp < latestTimestamp.Subtract(TimeSpan.FromHours(48))).ToArray();
            var last72hrSales = last72hrs.Sum(x => x.Quantity);
            var last72hrValue = (long)Math.Round(last72hrs.Length > 0 ? last72hrs.Average(x => x.Price) : 0, 0);
            var last96hrs = salesSorted.Where(x => x.Timestamp >= latestTimestamp.Subtract(TimeSpan.FromHours(96)) && x.Timestamp < latestTimestamp.Subtract(TimeSpan.FromHours(72))).ToArray();
            var last96hrSales = last96hrs.Sum(x => x.Quantity);
            var last96hrValue = (long)Math.Round(last96hrs.Length > 0 ? last96hrs.Average(x => x.Price) : 0, 0);
            var last120hrs = salesSorted.Where(x => x.Timestamp >= latestTimestamp.Subtract(TimeSpan.FromHours(120)) && x.Timestamp < latestTimestamp.Subtract(TimeSpan.FromHours(96))).ToArray();
            var last120hrSales = last120hrs.Sum(x => x.Quantity);
            var last120hrValue = (long)Math.Round(last120hrs.Length > 0 ? last120hrs.Average(x => x.Price) : 0, 0);
            var last144hrs = salesSorted.Where(x => x.Timestamp >= latestTimestamp.Subtract(TimeSpan.FromHours(144)) && x.Timestamp < latestTimestamp.Subtract(TimeSpan.FromHours(120))).ToArray();
            var last144hrSales = last144hrs.Sum(x => x.Quantity);
            var last144hrValue = (long)Math.Round(last144hrs.Length > 0 ? last144hrs.Average(x => x.Price) : 0, 0);
            var last168hrs = salesSorted.Where(x => x.Timestamp >= latestTimestamp.Subtract(TimeSpan.FromHours(168)) && x.Timestamp < latestTimestamp.Subtract(TimeSpan.FromHours(144))).ToArray();
            var last168hrSales = last168hrs.Sum(x => x.Quantity);
            var last168hrValue = (long)Math.Round(last168hrs.Length > 0 ? last168hrs.Average(x => x.Price) : 0, 0);

            FirstSeenOn = salesSorted.FirstOrDefault()?.Timestamp;
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

            // The first three days sees alot of extreme price spikes, filter these out of the overall averages
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

            if (sales != null)
            {
                SalesHistory.Clear();
                foreach (var sale in salesSorted)
                {
                    SalesHistory.Add(sale);
                }
            }
        }
    }
}
