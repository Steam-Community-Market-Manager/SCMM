using SCMM.Steam.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public class SteamMarketItem : SteamItem
    {
        public SteamMarketItem()
        {
            BuyOrders = new Collection<SteamMarketItemOrder>();
            SellOrders = new Collection<SteamMarketItemOrder>();
            SalesHistory = new Collection<SteamMarketItemSale>();
        }

        public Guid? CurrencyId { get; set; }

        public SteamCurrency Currency { get; set; }

        public ICollection<SteamMarketItemOrder> BuyOrders { get; set; }

        public ICollection<SteamMarketItemOrder> SellOrders { get; set; }

        public ICollection<SteamMarketItemSale> SalesHistory { get; set; }

        // What is the sum quantity of all sell orders
        public int Supply { get; set; }

        // What is the total number of unique sell orders
        public int SupplyUnique { get; set; }

        // What is the sum quantity of all buy orders
        public int Demand { get; set; }

        // What is the total number of unique buy orders
        public int DemandUnique { get; set; }

        // What is the cheapest buy order
        public int BuyAskingPrice { get; set; }

        // What is the cheapest sell order
        public int BuyNowPrice { get; set; }

        // What is the difference between the cheapest and 2nd cheapest sell orders
        public int BuyNowPriceDelta { get; set; }

        // What is the price you could reasonably flip this for given the current buy orders
        public int ResellPrice { get; set; }

        // What tax is owed on resell price
        public int ResellTax { get; set; }

        // What is the difference between buy now and resell prices
        public int ResellProfit { get; set; }

        [NotMapped]
        public bool WouldResellProfit => (ResellProfit >= 0);

        [NotMapped]
        public bool WouldResellLoss => (ResellProfit < 0);

        // What was the average price from the first 24hrs (1 day)
        public int First24hrValue { get; set; }

        // What was the total number of sales from the last 24hrs (1 day)
        public int Last24hrSales { get; set; }

        // What was the average price from the last 24hrs (1 day)
        public int Last24hrValue { get; set; }

        // What was the total number of sales from the last 48hrs (2 days)
        public int Last48hrSales { get; set; }

        // What was the average price from the last 48hrs (2 days)
        public int Last48hrValue { get; set; }

        // What was the total number of sales from the last 48hrs (5 days)
        public int Last120hrSales { get; set; }

        // What was the average price from the last 120hrs (5 days)
        public int Last120hrValue { get; set; }

        // What was the total number of sales from the last 336hrs (14 days)
        public int Last336hrSales { get; set; }

        // What was the average price from the last 336hrs (14 days)
        public int Last336hrValue { get; set; }

        // What is the difference between current and 48hr sale prices
        [NotMapped]
        public int MovementLast48hrValue => (Last24hrValue - Last48hrValue);

        // What is the difference between current and 120hr sale prices
        [NotMapped]
        public int MovementLast120hrValue => (Last24hrValue - Last120hrValue);

        // What is the difference between current and 336hr sale prices
        [NotMapped]
        public int MovementLast336hrValue => (Last24hrValue - Last336hrValue);

        // What is the difference between current and original sale prices
        [NotMapped]
        public int MovementAllTimeValue => (Last24hrValue - First24hrValue);

        [NotMapped]
        public bool HasAppreciated => (Last24hrValue >= First24hrValue);

        [NotMapped]
        public bool HasDepreciated => (Last24hrValue < First24hrValue);

        // What was the all-time highest price this ever sold for
        public int AllTimeHighestValue { get; set; }

        public DateTimeOffset? AllTimeHighestValueOn { get; set; }

        // What was the all-time lowest price this ever sold for
        public int AllTimeLowestValue { get; set; }

        public DateTimeOffset? AllTimeLowestValueOn { get; set; }

        // What is the difference between all-time highest and lowest sale prices
        public int AllTimeSwingValue => (AllTimeHighestValue - AllTimeLowestValue);

        // When was the very first sale
        public DateTimeOffset? FirstSeenOn { get; set; }

        public TimeSpan? MarketAge => (DateTimeOffset.Now - FirstSeenOn);

        // How long since orders were last checked
        public DateTimeOffset? LastCheckedOrdersOn { get; set; }

        // How long since prices were last checked
        public DateTimeOffset? LastCheckedSalesOn { get; set; }

        public void RecalculateOrders(SteamMarketItemOrder[] buyOrders = null, SteamMarketItemOrder[] sellOrders = null)
        {
            var buyOrdersSafe = (buyOrders ?? BuyOrders?.ToArray());
            if (buyOrdersSafe != null)
            {
                var buyOrdersSorted = buyOrdersSafe.OrderByDescending(y => y.Price).ToArray();
                var highestPrice = (buyOrdersSorted.Length > 0)
                    ? buyOrdersSorted.First().Price
                    : 0;

                Demand = buyOrdersSorted.Sum(y => y.Quantity);
                DemandUnique = buyOrdersSorted.Length;
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
                var lowestPrice = (sellOrdersSorted.Length > 0)
                    ? sellOrdersSorted.First().Price
                    : 0;
                var secondLowestPrice = (sellOrdersSorted.Length > 1)
                    ? sellOrdersSorted.Skip(1).First().Price
                    : lowestPrice;
                var averagePrice = (sellOrdersSorted.Length > 1)
                    ? (int)Math.Ceiling((decimal)sellOrdersSorted.Skip(1).Sum(y => y.Price) / (sellOrdersSorted.Length - 1))
                    : 0;
                var resellPrice = secondLowestPrice;
                var resellTaxSteam = Math.Max(1, (int)Math.Round(resellPrice * SteamEconomyHelper.DefaultSteamFeeMultiplier, 0));
                var resellTaxPublisher = Math.Max(1, (int)Math.Round(resellPrice * SteamEconomyHelper.DefaultPublisherFeeMultiplier, 0));
                var resellTax = (resellTaxSteam + resellTaxPublisher);

                Supply = sellOrdersSorted.Sum(y => y.Quantity);
                SupplyUnique = sellOrdersSorted.Length;
                BuyNowPrice = lowestPrice;
                BuyNowPriceDelta = (secondLowestPrice - lowestPrice);
                ResellPrice = resellPrice;
                ResellTax = resellTax;
                ResellProfit = (resellPrice - resellTax - lowestPrice);
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
            var earliestTimestamp = salesSorted.Min(x => x.Timestamp);
            var latestTimestamp = salesSorted.Max(x => x.Timestamp);
            var currentTimeStamp = DateTimeOffset.UtcNow;
            var first24hrs = salesSorted.Where(x => x.Timestamp < earliestTimestamp.Add(TimeSpan.FromHours(24))).ToArray();
            var first24hrValue = (int) Math.Round(first24hrs.Length > 0 ? first24hrs.Average(x => x.Price) : 0, 0);
            var last24hrs = salesSorted.Where(x => x.Timestamp > currentTimeStamp.Subtract(TimeSpan.FromHours(24))).ToArray();
            var last24hrSales = last24hrs.Sum(x => x.Quantity);
            var last24hrValue = (int) Math.Round(last24hrs.Length > 0 ? last24hrs.Average(x => x.Price) : 0, 0);
            var last48hrs = salesSorted.Where(x => x.Timestamp > currentTimeStamp.Subtract(TimeSpan.FromHours(48))).ToArray();
            var last48hrSales = last48hrs.Sum(x => x.Quantity);
            var last48hrValue = (int) Math.Round(last48hrs.Length > 0 ? last48hrs.Average(x => x.Price) : 0, 0);
            var last120hrs = salesSorted.Where(x => x.Timestamp > currentTimeStamp.Subtract(TimeSpan.FromHours(120))).ToArray();
            var last120hrSales = last120hrs.Sum(x => x.Quantity);
            var last120hrValue = (int) Math.Round(last120hrs.Length > 0 ? last120hrs.Average(x => x.Price) : 0, 0);
            var last336hrs = salesSorted.Where(x => x.Timestamp > currentTimeStamp.Subtract(TimeSpan.FromHours(336))).ToArray();
            var last336hrSales = last336hrs.Sum(x => x.Quantity);
            var last336hrValue = (int)Math.Round(last336hrs.Length > 0 ? last336hrs.Average(x => x.Price) : 0, 0);
            var allTimeLow = salesSorted.FirstOrDefault(x => x.Price == salesSorted.Min(x => x.Price));
            var allTimeHigh = salesSorted.FirstOrDefault(x => x.Price == salesSorted.Max(x => x.Price));

            First24hrValue = first24hrValue;
            Last24hrSales = last24hrSales;
            Last24hrValue = last24hrValue;
            Last48hrSales = last48hrSales;
            Last48hrValue = last48hrValue;
            Last120hrSales = last48hrSales;
            Last120hrValue = last120hrValue;
            Last336hrSales = last48hrSales;
            Last336hrValue = last336hrValue;
            AllTimeHighestValue = (allTimeHigh?.Price ?? 0);
            AllTimeHighestValueOn = allTimeHigh?.Timestamp;
            AllTimeLowestValue = (allTimeLow?.Price ?? 0);
            AllTimeLowestValueOn = allTimeLow?.Timestamp;
            FirstSeenOn = salesSorted.FirstOrDefault()?.Timestamp;
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
