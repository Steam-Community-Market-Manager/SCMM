namespace SCMM.Web.Data.Models.UI.System;

public class TimeRangeWithTargetDTO : TimeRangeDTO
{
    public TimeSpan? TargetDelta { get; set; }

    public bool IsOnTarget => (
        Newest != null && Oldest != null && (DateTimeOffset.Now - Oldest)?.Duration() <= TargetDelta && Delta <= TargetDelta
    );
}
