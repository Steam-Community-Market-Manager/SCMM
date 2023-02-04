using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using System.Reflection;

namespace SCMM.Steam.Data.Models.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class SellToAttribute : Attribute
{
    public string Url { get; set; }

    public string AffiliateCode { get; set; }

    public bool HasAffiliateProgram => !String.IsNullOrEmpty(AffiliateCode);

    public PriceTypes AcceptedPaymentTypes { get; set; }

    public long FeeSurcharge { get; set; }

    public float FeeRate { get; set; }

    public long CalculateBuyFees(long price)
    {
        var fee = 0L;
        if (FeeRate != 0 && price > 0)
        {
            fee += price.MarketSaleFeeComponentAsInt(FeeRate);
        }
        if (FeeSurcharge != 0 && price > 0)
        {
            fee += FeeSurcharge;
        }

        return fee;
    }

    public string GenerateBuyUrl(string appId, string appName, ulong? classId, string name)
    {
        return String.Format((Url ?? String.Empty),
            Uri.EscapeDataString(appId ?? String.Empty), Uri.EscapeDataString(appName?.ToLower() ?? String.Empty), classId, Uri.EscapeDataString(name ?? String.Empty)
        );
    }
}
