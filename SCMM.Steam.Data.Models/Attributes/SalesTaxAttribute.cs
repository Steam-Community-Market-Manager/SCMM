namespace SCMM.Steam.Data.Models.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class SalesTaxAttribute : Attribute
{
    public SalesTaxAttribute(float tax)
    {
        Tax = tax;
    }

    public float Tax { get; set; }
}
