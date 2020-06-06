using SCMM.Steam.Shared;
using SCMM.Web.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace SCMM.Web.Server.Domain.Models.Steam
{
    public class SteamItem : Entity
    {
        public SteamItem()
        {
            BuyOrders = new Collection<SteamItemOrder>();
            SellOrders = new Collection<SteamItemOrder>();
        }

        public string SteamId { get; set; }

        [Required]
        public Guid AppId { get; set; }

        public SteamApp App { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public Guid DescriptionId { get; set; }

        public SteamItemDescription Description { get; set; }

        public Guid? CurrencyId { get; set; }

        public SteamCurrency Currency { get; set; }

        public ICollection<SteamItemOrder> BuyOrders { get; set; }

        public ICollection<SteamItemOrder> SellOrders { get; set; }

        public int Supply { get; set; }

        public int Demand { get; set; }

        public int SellLowestPrice { get; set; }

        public int SellLowestDelta { get; set; }

        public int ResellPrice { get; set; }

        public int ResellTax { get; set; }

        public int ResellProfit { get; set; }

        [NotMapped]
        public bool IsResellProfit => (ResellProfit >= 0);

        [NotMapped]
        public bool IsResellLoss => (ResellProfit < 0);

        public DateTimeOffset LastChecked { get; set; }

        public TimeSpan LastCheckedAgo => (DateTimeOffset.Now - LastChecked);

        public void RebuildOrders(SteamItemOrder[] buyOrders, SteamItemOrder[] sellOrders)
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
                var resellTaxSteam = Math.Max(1, (int)Math.Round(resellPrice * SteamConstants.FeeSteamMultiplier, 0));
                var resellTaxPublisher = Math.Max(1, (int)Math.Round(resellPrice * SteamConstants.FeePublisherMultiplier, 0));
                var resellTax = (resellTaxSteam + resellTaxPublisher);

                Supply = sellOrders.Sum(y => y.Quantity);
                SellLowestPrice = lowestListPrice;
                SellLowestDelta = (secondLowestListPrice - lowestListPrice);
                ResellPrice = resellPrice;
                ResellTax = resellTax;
                ResellProfit = (resellPrice - resellTax - lowestListPrice);
                SellOrders.Clear();
                foreach(var order in sellOrdersSorted)
                {
                    SellOrders.Add(order);
                }
            }
        }
    }
}
