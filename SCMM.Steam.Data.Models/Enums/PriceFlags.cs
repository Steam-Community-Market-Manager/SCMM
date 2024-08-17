namespace SCMM.Steam.Data.Models.Enums
{
    [Flags]
    public enum PriceFlags : byte
    {
        Trade = 0x01,
        Cash = 0x02,
        Crypto = 0x04
    }
}
