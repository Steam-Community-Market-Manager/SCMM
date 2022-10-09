namespace SCMM.Shared.Abstractions.Analytics;

public interface ITimeSeriesAnalysisService
{
    Task<IEnumerable<ITimeSeriesAnomaly>> DetectTimeSeriesAnomaliesAsync(IDictionary<DateTimeOffset, float> data, TimeGranularity? granularity = TimeGranularity.Daily, int? sensitivity = null);
}
