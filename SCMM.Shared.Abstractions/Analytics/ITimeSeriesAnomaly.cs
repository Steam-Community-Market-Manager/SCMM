namespace SCMM.Shared.Abstractions.Analytics;

public interface ITimeSeriesAnomaly
{
    public DateTimeOffset? Timestamp { get; }

    public float ActualValue { get; }

    public float ExpectedValue { get; }

    public float UpperMargin { get; }

    public float LowerMargin { get; }

    public bool IsNegative { get; }

    public bool IsPositive { get; }

    public float Severity { get; }
}
