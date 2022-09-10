namespace SCMM.Steam.Data.Models.Enums
{
    [Flags]
    public enum PriceTypes : byte
    {
        None = 0x00,
        Cash = 0x01,
        Trade = 0x02
    }
}
