using SCMM.Steam.Shared;
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
                var resellTaxSteam = Math.Max(1, (int)Math.Round(resellPrice * SteamConstants.DefaultSteamFeeMultiplier, 0));
                var resellTaxPublisher = Math.Max(1, (int)Math.Round(resellPrice * SteamConstants.DefaultPublisherFeeMultiplier, 0));
                var resellTax = (resellTaxSteam + resellTaxPublisher);

                /*
                    // Since CalculateFeeAmount has a Math.floor, we could be off a cent or two. Let's check:
                    var iterations = 0; // shouldn't be needed, but included to be sure nothing unforseen causes us to get stuck
                    var nEstimatedAmountOfWalletFundsReceivedByOtherParty = parseInt( ( amount - parseInt( g_rgWalletInfo['wallet_fee_base'] ) ) / ( parseFloat( g_rgWalletInfo['wallet_fee_percent'] ) + parseFloat( publisherFee ) + 1 ) );

                    var bEverUndershot = false;
                    var fees = CalculateAmountToSendForDesiredReceivedAmount( nEstimatedAmountOfWalletFundsReceivedByOtherParty, publisherFee );
                    while ( fees.amount != amount && iterations < 10 )
                    {
                        if ( fees.amount > amount )
                        {
                            if ( bEverUndershot )
                            {
                                fees = CalculateAmountToSendForDesiredReceivedAmount( nEstimatedAmountOfWalletFundsReceivedByOtherParty - 1, publisherFee );
                                fees.steam_fee += ( amount - fees.amount );
                                fees.fees += ( amount - fees.amount );
                                fees.amount = amount;
                                break;
                            }
                            else
                            {
                                nEstimatedAmountOfWalletFundsReceivedByOtherParty--;
                            }
                        }
                        else
                        {
                            bEverUndershot = true;
                            nEstimatedAmountOfWalletFundsReceivedByOtherParty++;
                        }

                        fees = CalculateAmountToSendForDesiredReceivedAmount( nEstimatedAmountOfWalletFundsReceivedByOtherParty, publisherFee );
                        iterations++;
                    }
                */
                /*
                    publisherFee = ( typeof publisherFee == 'undefined' ) ? 0 : publisherFee;

                    var nSteamFee = parseInt( Math.floor( Math.max( receivedAmount * parseFloat( g_rgWalletInfo['wallet_fee_percent'] ), g_rgWalletInfo['wallet_fee_minimum'] ) + parseInt( g_rgWalletInfo['wallet_fee_base'] ) ) );
                    var nPublisherFee = parseInt( Math.floor( publisherFee > 0 ? Math.max( receivedAmount * publisherFee, 1 ) : 0 ) );
                    var nAmountToSend = receivedAmount + nSteamFee + nPublisherFee;

                    return {
                        steam_fee: nSteamFee,
                        publisher_fee: nPublisherFee,
                        fees: nSteamFee + nPublisherFee,
                        amount: parseInt( nAmountToSend )
                    };
                */

                Supply = sellOrders.Sum(y => y.Quantity);
                SellLowestPrice = lowestListPrice;
                SellLowestDelta = (secondLowestListPrice - lowestListPrice);
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
