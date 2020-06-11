using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SCMM.Steam.Shared
{
    /// <summary>
    /// C# port of the Steam economy common logic
	/// https://steamcommunity-a.akamaihd.net/public/javascript/economy_common.js?v=tsXdRVB0yEaR&l=english
    /// </summary>
    public static class SteamEconomyHelper
	{
		public const decimal DefaultSteamFeeMultiplier = 0.05m;
		public const decimal DefaultPublisherFeeMultiplier = 0.100000001490116119m;

		/*
		public static int CalculateFeeAmount(int amount, int publisherFee)
		{
			if (!g_rgWalletInfo['wallet_fee'])
				return 0;

			publisherFee = (typeof publisherFee == 'undefined') ? 0 : publisherFee;

			// Since CalculateFeeAmount has a Math.floor, we could be off a cent or two. Let's check:
			var iterations = 0; // shouldn't be needed, but included to be sure nothing unforseen causes us to get stuck
			var nEstimatedAmountOfWalletFundsReceivedByOtherParty = parseInt((amount - parseInt(g_rgWalletInfo['wallet_fee_base'])) / (parseFloat(g_rgWalletInfo['wallet_fee_percent']) + parseFloat(publisherFee) + 1));

			var bEverUndershot = false;
			var fees = CalculateAmountToSendForDesiredReceivedAmount(nEstimatedAmountOfWalletFundsReceivedByOtherParty, publisherFee);
			while (fees.amount != amount && iterations < 10)
			{
				if (fees.amount > amount)
				{
					if (bEverUndershot)
					{
						fees = CalculateAmountToSendForDesiredReceivedAmount(nEstimatedAmountOfWalletFundsReceivedByOtherParty - 1, publisherFee);
						fees.steam_fee += (amount - fees.amount);
						fees.fees += (amount - fees.amount);
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

				fees = CalculateAmountToSendForDesiredReceivedAmount(nEstimatedAmountOfWalletFundsReceivedByOtherParty, publisherFee);
				iterations++;
			}

			// fees.amount should equal the passed in amount

			return fees;
		}

		public static int CalculateAmountToSendForDesiredReceivedAmount(int receivedAmount, int publisherFee )
		{
			if (!g_rgWalletInfo['wallet_fee'])
			{
				return receivedAmount;
			}

			publisherFee = (typeof publisherFee == 'undefined') ? 0 : publisherFee;

			var nSteamFee = parseInt(Math.floor(Math.max(receivedAmount * parseFloat(g_rgWalletInfo['wallet_fee_percent']), g_rgWalletInfo['wallet_fee_minimum']) + parseInt(g_rgWalletInfo['wallet_fee_base'])));
			var nPublisherFee = parseInt(Math.floor(publisherFee > 0 ? Math.max(receivedAmount * publisherFee, 1) : 0));
			var nAmountToSend = receivedAmount + nSteamFee + nPublisherFee;

			return {
			steam_fee: nSteamFee,
		publisher_fee: nPublisherFee,
		fees: nSteamFee + nPublisherFee,
		amount: parseInt(nAmountToSend)
			};
		}

		public static int GetPriceValueAsInt(string strAmount)
		{
			var nAmount = 0;
			if (String.IsNullOrEmpty(strAmount))
			{
				return 0;
			}

			// Users may enter either comma or period for the decimal mark and digit group separators.
			strAmount = strAmount.Replace(',', '.');

			// strip the currency symbol, set .-- to .00
			strAmount = Regex.Match(strAmount, @"([\d\.]+)").Groups.OfType<Capture>().LastOrDefault()?.Value;
			strAmount = strAmount.Replace(".--", ".00");

			// strip spaces
			strAmount = strAmount.Replace(" ", String.Empty);

			// Remove all but the last period so that entries like "1,147.6" work
			if (strAmount.IndexOf('.') != -1)
			{
				var splitAmount = strAmount.Split('.');
				var strLastSegment = splitAmount.Length > 0 ? splitAmount[splitAmount.Length - 1] : null;

				if (!String.IsNullOrEmpty(strLastSegment) && strLastSegment.Length == 3 && Int32.Parse(splitAmount[splitAmount.Length - 2]) != 0)
				{
					// Looks like the user only entered thousands separators. Remove all commas and periods.
					// Ensures an entry like "1,147" is not treated as "1.147"
					//
					// Users may be surprised to find that "1.147" is treated as "1,147". "1.147" is either an error or the user
					// really did mean one thousand one hundred and forty seven since no currencies can be split into more than
					// hundredths. If it was an error, the user should notice in the next step of the dialog and can go back and
					// correct it. If they happen to not notice, it is better that we list the item at a higher price than
					// intended instead of lower than intended (which we would have done if we accepted the 1.147 value as is).
					strAmount = String.Join(String.Empty, splitAmount);
				}
				else
				{
					strAmount = String.Join(String.Empty, splitAmount.Skip(1)) + '.' + strLastSegment;
				}
			}

			var flAmount = float.Parse(strAmount) * 100;
			nAmount = (int) Math.Floor(flAmount + 0.000001f); // round down

			nAmount = Math.Max(nAmount, 0);
			return nAmount;
		}
		*/
	}
}
