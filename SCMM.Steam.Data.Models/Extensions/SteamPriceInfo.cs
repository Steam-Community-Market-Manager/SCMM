namespace SCMM.Steam.Data.Models.Extensions;

public class SteamPriceInfo
{
    public string Currency { get; set; }

    public ulong Price { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}
