using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Steam.Data.Models.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class MarketAttribute : Attribute
{
    public MarketAttribute(params ulong[] supportAppIds)
    {
        SupportedAppIds = supportAppIds;
    }

    public ulong[] SupportedAppIds { get; set; }

    public PriceTypes Type { get; set; }

    public string Color { get; set; }
}
