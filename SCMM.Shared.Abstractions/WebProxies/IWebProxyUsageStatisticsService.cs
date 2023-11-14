namespace SCMM.Shared.Abstractions.WebProxies;

public interface IWebProxyUsageStatisticsService
{
    Task<IEnumerable<WebProxyWithUsageStatistics>> GetAsync();

    Task SetAsync(IEnumerable<WebProxyWithUsageStatistics> statistics);

    Task PatchAsync(string proxyUrl, Action<WebProxyWithUsageStatistics> updateAction);
}
