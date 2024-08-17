using SCMM.Shared.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;

namespace SCMM.Steam.Data.Models.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class BuyFromAttribute : Attribute
{
    public string Url { get; set; }

    public PriceFlags AcceptedPayments { get; set; }

    /// <summary>
    /// Multiplier of "bonus balance" earned from buying market site balance
    /// </summary>
    public float BonusBalanceMultiplier { get; set; }

    /// <summary>
    /// Fixed surcharge amount to add to the price
    /// </summary>
    public long SurchargeFixedAmount { get; set; }

    /// <summary>
    /// Percentage surcharge amount to add to the price
    /// </summary>
    public float SurchargePercentage { get; set; }

    /// <summary>
    /// If the market uses an in-house currency, this is the description (e.g. "coins")
    /// </summary>
    public string HouseCurrencyName { get; set; }

    /// <summary>
    /// If the market uses a in-house currency, this is the number of decimal places used (e.g. 2 for 2 decimal places)
    /// </summary>
    public int HouseCurrencyScale { get; set; }

    /// <summary>
    /// If the market uses a in-house currency, this is the exchange rate to convert it to USD
    /// </summary>
    public double HouseCurrencyToUsdExchangeRate { get; set; }

    public long CalculateBuyPrice(long price)
    {
        var buyPrice = price;
        return Math.Max(0, buyPrice);
    }

    public long CalculateBuyFees(long price)
    {
        var buyFees = 0L;
        if (BonusBalanceMultiplier > 0 && price > 0)
        {
            buyFees -= (long)Math.Round(price - (price * BonusBalanceMultiplier), 0);
        }
        if (SurchargePercentage != 0 && price > 0)
        {
            buyFees += (long)Math.Round(price * SurchargePercentage, 0);
        }
        if (SurchargeFixedAmount != 0 && price > 0)
        {
            buyFees += SurchargeFixedAmount;
        }

        return buyFees;
    }

    public string GenerateBuyUrl(string appId, string appName, ulong? classId, string name)
    {
        return String.Format((Url ?? String.Empty),
            Uri.EscapeDataString(appId ?? String.Empty), Uri.EscapeDataString(appName?.ToLower() ?? String.Empty), classId, Uri.EscapeDataString(name ?? String.Empty)
        );
    }

    public IExchangeableCurrency GetHouseCurrency()
    {
        if (!String.IsNullOrEmpty(HouseCurrencyName) && HouseCurrencyToUsdExchangeRate > 0)
        {
            return new MarketHouseCurrency()
            {
                Id = 0,
                Name = HouseCurrencyName,
                SuffixText = HouseCurrencyName.ToLower(),
                Scale = HouseCurrencyScale,
                ExchangeRateMultiplier = (Constants.SteamDefaultCurrencyExchangeRate / (decimal)HouseCurrencyToUsdExchangeRate)
            };
        }

        return null;
    }

    public class MarketHouseCurrency : IExchangeableCurrency
    {
        public uint Id { get; set; }

        public string Name { get; set; }

        public string PrefixText { get; set; }

        public string SuffixText { get; set; }

        public string CultureName { get; set; }

        public int Scale { get; set; }

        public decimal ExchangeRateMultiplier { get; set; }
    }
}
