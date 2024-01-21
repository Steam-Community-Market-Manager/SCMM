namespace SCMM.Steam.Data.Models.Enums
{
    [Flags]
    public enum PriceFlags : byte
    {
        None = 0x00,
        Trade = 0x01,
        Cash = 0x02,
        Crypto = 0x04
    }
}
