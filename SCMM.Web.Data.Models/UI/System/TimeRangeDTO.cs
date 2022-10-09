namespace SCMM.Web.Data.Models.UI.System;

public class TimeRangeDTO
{
    public DateTimeOffset? Oldest { get; set; }

    public DateTimeOffset? Newest { get; set; }

    public TimeSpan? Delta => (Newest - Oldest)?.Duration();
}
