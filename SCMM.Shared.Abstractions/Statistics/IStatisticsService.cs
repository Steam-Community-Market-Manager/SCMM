namespace SCMM.Shared.Abstractions.Statistics;

public interface IStatisticsService
{
    Task<T> GetAsync<T>(string key);

    Task<bool> SetAsync<T>(string key, T stat);

    Task<bool> ModifyAsync<T>(string key, Func<T, T> updateFunc);
}
