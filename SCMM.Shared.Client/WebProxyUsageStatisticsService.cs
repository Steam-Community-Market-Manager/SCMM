using SCMM.Shared.Abstractions.Statistics;
using SCMM.Shared.Abstractions.WebProxies;

namespace SCMM.Shared.Client;

public class WebProxyUsageStatisticsService : IWebProxyUsageStatisticsService
{
    private const string WebProxiesStatsKey = "web-proxies";

    private readonly IStatisticsService _statisticsService;

    public WebProxyUsageStatisticsService(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public async Task<IEnumerable<WebProxyWithUsageStatistics>> GetAsync()
    {
        return (await _statisticsService.GetDictionaryAsync<string, WebProxyWithUsageStatistics>(WebProxiesStatsKey) ?? new Dictionary<string, WebProxyWithUsageStatistics>())
            .Select(x => x.Value)
            .ToArray();
    }

    public async Task SetAsync(IEnumerable<WebProxyWithUsageStatistics> statistics)
    {
        await _statisticsService.SetDictionaryAsync<string, WebProxyWithUsageStatistics>(
            WebProxiesStatsKey,
            statistics.ToDictionary(k => k.Url, v => v),
            deleteKeyBeforeSet: true
        );
    }

    public async Task PatchAsync(string proxyUrl, Action<WebProxyWithUsageStatistics> updateAction)
    {
        await _statisticsService.PatchDictionaryValueAsync<string, WebProxyWithUsageStatistics>(
            WebProxiesStatsKey, proxyUrl, updateAction
        );
    }

}
