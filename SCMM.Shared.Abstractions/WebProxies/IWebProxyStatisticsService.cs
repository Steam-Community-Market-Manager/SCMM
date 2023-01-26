namespace SCMM.Shared.Abstractions.WebProxies;

public interface IWebProxyStatisticsService
{
    Task<IEnumerable<WebProxyStatistic>> GetAllStatisticsAsync();

    Task SetAllStatistics(IEnumerable<WebProxyStatistic> statistics);

    Task UpdateStatisticsAsync(string proxyUrl, Action<WebProxyStatistic> updateAction);
}
