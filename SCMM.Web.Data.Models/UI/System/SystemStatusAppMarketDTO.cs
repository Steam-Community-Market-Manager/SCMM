namespace SCMM.Web.Data.Models.UI.System;

public class SystemStatusAppMarketDTO
{
    public int TotalItems { get; set; }

    public long TotalListings { get; set; }

    public DateTimeOffset? LastUpdatedItemsOn { get; set; }

    public DateTimeOffset? LastUpdateErrorOn { get; set; }

    public string LastUpdateError { get; set; }

    public SystemStatusSeverity Status
    {
        get
        {
            if (LastUpdateErrorOn != null)
            {
                return SystemStatusSeverity.Degraded;
            }
            else
            {
                return SystemStatusSeverity.Normal;
            }
        }
    }

}
