namespace SCMM.Steam.Data.Models.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class SellToAttribute : Attribute
{
    public string Url { get; set; }

    public float Tax { get; set; }
}
