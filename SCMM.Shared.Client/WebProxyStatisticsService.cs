using SCMM.Shared.Abstractions.Statistics;
using SCMM.Shared.Abstractions.WebProxies;

namespace SCMM.Shared.Client;

public class WebProxyStatisticsService : IWebProxyStatisticsService
{
    private const string WebProxiesStatsKey = "web-proxies";

    private readonly IStatisticsService _statisticsService;

    public WebProxyStatisticsService(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public async Task<IEnumerable<WebProxyStatistic>> GetAllStatisticsAsync()
    {
        return (await _statisticsService.GetDictionaryAsync<string, WebProxyStatistic>(WebProxiesStatsKey) ?? new Dictionary<string, WebProxyStatistic>())
            .Select(x => x.Value)
            .ToArray();
    }

    public async Task SetAllStatisticsAsync(IEnumerable<WebProxyStatistic> statistics)
    {
        await _statisticsService.SetDictionaryAsync<string, WebProxyStatistic>(
            WebProxiesStatsKey,
            statistics.ToDictionary(k => k.Url, v => v),
            deleteKeyBeforeSet: true
        );
    }

    public async Task UpdateStatisticsAsync(string proxyUrl, Action<WebProxyStatistic> updateAction)
    {
        await _statisticsService.UpdateDictionaryValueAsync<string, WebProxyStatistic>(
            WebProxiesStatsKey, proxyUrl, updateAction
        );
    }

}
