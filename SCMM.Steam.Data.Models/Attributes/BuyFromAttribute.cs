using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;

namespace SCMM.Steam.Data.Models.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class BuyFromAttribute : Attribute
{
    public string Url { get; set; }

    public PriceFlags AcceptedPayments { get; set; }

    public float DiscountMultiplier { get; set; }

    public long FeeSurcharge { get; set; }

    public float FeeRate { get; set; }

    public long CalculateBuyPrice(long price)
    {
        var buyPrice = price;
        if (DiscountMultiplier > 0 && buyPrice > 0)
        {
            buyPrice -= (long)Math.Round(buyPrice * DiscountMultiplier, 0);
        }

        return Math.Max(0, buyPrice);
    }

    public long CalculateBuyFees(long price)
    {
        var buyFees = 0L;
        if (FeeRate != 0 && price > 0)
        {
            buyFees += price.MarketSaleFeeComponentAsInt(FeeRate);
        }
        if (FeeSurcharge != 0 && price > 0)
        {
            buyFees += FeeSurcharge;
        }

        return buyFees;
    }

    public string GenerateBuyUrl(string appId, string appName, ulong? classId, string name)
    {
        return String.Format((Url ?? String.Empty),
            Uri.EscapeDataString(appId ?? String.Empty), Uri.EscapeDataString(appName?.ToLower() ?? String.Empty), classId, Uri.EscapeDataString(name ?? String.Empty)
        );
    }
}
