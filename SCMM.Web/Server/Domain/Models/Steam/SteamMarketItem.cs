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
        }

        public Guid? CurrencyId { get; set; }

        public SteamCurrency Currency { get; set; }

        public ICollection<SteamMarketItemOrder> BuyOrders { get; set; }

        public ICollection<SteamMarketItemOrder> SellOrders { get; set; }

        public int Supply { get; set; }

        public int Demand { get; set; }

        public int BuyNowPrice { get; set; }

        public int BuyNowPriceDelta { get; set; }

        public int ResellPrice { get; set; }

        public int ResellTax { get; set; }

        public int ResellProfit { get; set; }

        [NotMapped]
        public bool IsResellProfit => (ResellProfit >= 0);

        [NotMapped]
        public bool IsResellLoss => (ResellProfit < 0);

        public DateTimeOffset? LastCheckedOn { get; set; }

        public void RebuildOrders(SteamMarketItemOrder[] buyOrders, SteamMarketItemOrder[] sellOrders)
        {
            if (buyOrders != null)
            {
                var buyOrdersSorted = buyOrders.OrderBy(y => y.Price).ToArray();

                Demand = buyOrders.Sum(y => y.Quantity);
                BuyOrders.Clear();
                foreach (var order in buyOrdersSorted)
                {
                    BuyOrders.Add(order);
                }
            }
            if (sellOrders != null)
            {
                var sellOrdersSorted = sellOrders.OrderBy(y => y.Price).ToArray();
                var lowestListPrice = (sellOrdersSorted.Length > 0)
                    ? sellOrdersSorted.First().Price
                    : 0;
                var secondLowestListPrice = (sellOrdersSorted.Length > 1)
                    ? sellOrdersSorted.Skip(1).First().Price
                    : lowestListPrice;
                var averageListPrice = (sellOrdersSorted.Length > 1)
                    ? (int)Math.Ceiling((decimal)sellOrdersSorted.Skip(1).Sum(y => y.Price) / (sellOrdersSorted.Length - 1))
                    : 0;
                var resellPrice = secondLowestListPrice;
                var resellTaxSteam = Math.Max(1, (int)Math.Round(resellPrice * SteamEconomyHelper.DefaultSteamFeeMultiplier, 0));
                var resellTaxPublisher = Math.Max(1, (int)Math.Round(resellPrice * SteamEconomyHelper.DefaultPublisherFeeMultiplier, 0));
                var resellTax = (resellTaxSteam + resellTaxPublisher);

                Supply = sellOrders.Sum(y => y.Quantity);
                BuyNowPrice = lowestListPrice;
                BuyNowPriceDelta = (secondLowestListPrice - lowestListPrice);
                ResellPrice = resellPrice;
                ResellTax = resellTax;
                ResellProfit = (resellPrice - resellTax - lowestListPrice);
                SellOrders.Clear();
                foreach (var order in sellOrdersSorted)
                {
                    SellOrders.Add(order);
                }
            }
        }
    }
}
