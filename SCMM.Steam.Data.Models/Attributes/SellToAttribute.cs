namespace SCMM.Steam.Data.Models.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class SellToAttribute : Attribute
{
    public string Url { get; set; }

    public float FeeRate { get; set; }

    public long FeeSurcharge { get; set; }

    public string AffiliateCode { get; set; }

    public bool HasAffiliateProgram => !String.IsNullOrEmpty(AffiliateCode);
}
