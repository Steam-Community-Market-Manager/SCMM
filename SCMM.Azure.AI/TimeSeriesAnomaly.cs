namespace SCMM.Azure.AI;

public class TimeSeriesAnomaly
{
    public DateTimeOffset? Timestamp { get; set; }

    public float ActualValue { get; set; }

    public float ExpectedValue { get; set; }

    public float UpperMargin { get; set; }

    public float LowerMargin { get; set; }

    public bool IsNegative { get; set; }

    public bool IsPositive { get; set; }

    public float Severity { get; set; }
}
