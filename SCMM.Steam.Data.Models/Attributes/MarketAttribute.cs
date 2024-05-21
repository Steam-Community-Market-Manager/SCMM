namespace SCMM.Steam.Data.Models.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class MarketAttribute : Attribute
{
    public MarketAttribute(params ulong[] supportAppIds)
    {
        SupportedAppIds = supportAppIds;
    }

    public ulong[] SupportedAppIds { get; set; }

    public bool IsFirstParty { get; set; }

    public bool IsCasino { get; set; }

    public string Color { get; set; }

    public string AffiliateUrl { get; set; }
}
