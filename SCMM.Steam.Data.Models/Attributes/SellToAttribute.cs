using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;

namespace SCMM.Steam.Data.Models.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class SellToAttribute : Attribute
{
    public string Url { get; set; }

    public PriceFlags AcceptedPayments { get; set; }

    public long FeeSurcharge { get; set; }

    public float FeeRate { get; set; }

    public long CalculateSellPrice(long price)
    {
        var sellPrice = price;
        return Math.Max(0, sellPrice);
    }

    public long CalculateSellFees(long price)
    {
        var sellFees = 0L;
        if (FeeRate != 0 && price > 0)
        {
            sellFees += price.MarketSaleFeeComponentAsInt(FeeRate);
        }
        if (FeeSurcharge != 0 && price > 0)
        {
            sellFees += FeeSurcharge;
        }

        return sellFees;
    }

    public string GenerateBuyUrl(string appId, string appName, ulong? classId, string name)
    {
        return String.Format((Url ?? String.Empty),
            Uri.EscapeDataString(appId ?? String.Empty), Uri.EscapeDataString(appName?.ToLower() ?? String.Empty), classId, Uri.EscapeDataString(name ?? String.Empty)
        );
    }
}
