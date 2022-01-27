namespace SCMM.Steam.Data.Models.Enums
{
    [Flags]
    public enum SteamStoreTypes : byte
    {
        None = 0x00,
        Persistent = 0x01,
        Rotating = 0x02
    }
}
