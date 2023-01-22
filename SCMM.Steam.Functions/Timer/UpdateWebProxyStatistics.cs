using Microsoft.Azure.Functions.Worker;
using SCMM.Shared.Abstractions.Statistics;
using SCMM.Shared.Abstractions.WebProxies;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Models.Statistics;

namespace SCMM.Steam.Functions.Timer;

public class UpdateWebProxyStatistics
{
    private readonly IWebProxyManagementService _webProxies;
    private readonly IStatisticsService _statisticsService;

    public UpdateWebProxyStatistics(IWebProxyManagementService webProxies, IStatisticsService statisticsService)
    {
        _webProxies = webProxies;
        _statisticsService = statisticsService;
    }

    [Function("Update-Web-Proxy-Statistics")]
    public async Task Run([TimerTrigger("0 0 0/6 * * *")] /* every 6 hours, on the hour */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Web-Proxy-Statistics");

        // Get the latest list of proxies
        var webProxies = await _webProxies.ListWebProxiesAsync();
        if (webProxies != null)
        {
            // Get cached list
            var cachedWebProxies = new Dictionary<string, WebProxyStatistic>(
                await _statisticsService.GetDictionaryAsync<string, WebProxyStatistic>(StatisticKeys.WebProxies) ?? new Dictionary<string, WebProxyStatistic>()
            );

            // Remove all proxies that no longer exist
            cachedWebProxies.RemoveAll(x =>
                !webProxies.Any(y => y.Source == x.Value.Source && y.Id == x.Value.Id)
            );

            // Add or update all existing proxies
            foreach (var webProxy in webProxies)
            {
                var cachedWebProxy = cachedWebProxies.FirstOrDefault(x => x.Value.Source == webProxy.Source && x.Value.Id == webProxy.Id).Value;
                if (cachedWebProxy == null)
                {
                    cachedWebProxies.Add(
                        $"{webProxy.Address}:{webProxy.Port}",
                        cachedWebProxy = new WebProxyStatistic()
                        {
                            Source = webProxy.Source,
                            Id = webProxy.Id
                        }
                    );
                }

                cachedWebProxy.Address = webProxy.Address;
                cachedWebProxy.Port = webProxy.Port;
                cachedWebProxy.CountryCode = webProxy.CountryCode;
                cachedWebProxy.CityName = webProxy.CityName;
                cachedWebProxy.IsAvailable = webProxy.IsAvailable;
                cachedWebProxy.LastCheckedOn = webProxy.LastCheckedOn;
            }

            // Update cached list
            await _statisticsService.SetDictionaryAsync(
                StatisticKeys.WebProxies,
                cachedWebProxies, 
                deleteKeyBeforeSet: true
            );
        }
    }
}
