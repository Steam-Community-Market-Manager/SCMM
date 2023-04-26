using Microsoft.Azure.Functions.Worker;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.WebProxies;
using SCMM.Shared.API.Events;

namespace SCMM.Steam.Functions.Timer;

public class UpdateWebProxyStatistics
{
    private readonly IWebProxyManagementService _webProxies;
    private readonly IWebProxyStatisticsService _webProxyStatisticsService;
    private readonly IServiceBus _serviceBus;

    public UpdateWebProxyStatistics(IWebProxyManagementService webProxies, IWebProxyStatisticsService webProxyStatisticsService, IServiceBus serviceBus)
    {
        _webProxies = webProxies;
        _webProxyStatisticsService = webProxyStatisticsService;
        _serviceBus = serviceBus;
    }

    [Function("Update-Web-Proxy-Statistics")]
    public async Task Run([TimerTrigger("0 0 * * * *")] /* every hour, on the hour */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Web-Proxy-Statistics");
        var proxiesHaveChanged = false;

        // Get the latest list of proxies
        var webProxies = await _webProxies.ListWebProxiesAsync();
        if (webProxies != null)
        {
            // Get cached list
            var cachedWebProxies = new List<WebProxyStatistic>(
                await _webProxyStatisticsService.GetAllStatisticsAsync() ?? Enumerable.Empty<WebProxyStatistic>()
            );

            // Remove all empty proxies
            var emptyProxiesRemoved = cachedWebProxies.RemoveAll(x =>
                String.IsNullOrEmpty(x.Address) || x.Port <= 0
            );
            if (emptyProxiesRemoved > 0)
            {
                proxiesHaveChanged = true;
            }

            // Remove all proxies that no longer exist
            var deadProxiesRemoved = cachedWebProxies.RemoveAll(x =>
                !webProxies.Any(y => y.Source == x.Source && y.Id == x.Id)
            );
            if (deadProxiesRemoved > 0)
            {
                proxiesHaveChanged = true;
            }

            // Add or update all existing proxies
            foreach (var webProxy in webProxies)
            {
                var cachedWebProxy = cachedWebProxies.FirstOrDefault(x => x.Source == webProxy.Source && x.Id == webProxy.Id);
                if (cachedWebProxy == null)
                {
                    proxiesHaveChanged = true;
                    cachedWebProxies.Add(
                        cachedWebProxy = new WebProxyStatistic()
                        {
                            Source = webProxy.Source,
                            Id = webProxy.Id
                        }
                    );
                }

                cachedWebProxy.Source = webProxy.Source;
                cachedWebProxy.Id = webProxy.Id;
                cachedWebProxy.Address = webProxy.Address;
                cachedWebProxy.Port = webProxy.Port;
                cachedWebProxy.Username = webProxy.Username;
                cachedWebProxy.Password = webProxy.Password;
                cachedWebProxy.CountryCode = webProxy.CountryCode;
                cachedWebProxy.CityName = webProxy.CityName;
                cachedWebProxy.IsAvailable = webProxy.IsAvailable;
                cachedWebProxy.LastCheckedOn = webProxy.LastCheckedOn;
            }

            // Update cached list
            await _webProxyStatisticsService.SetAllStatisticsAsync(
                cachedWebProxies.Where(x => !String.IsNullOrEmpty(x.Address) && x.Port > 0)
            );

            if (proxiesHaveChanged)
            {
                await _serviceBus.SendMessageAsync(
                    new WebProxyStatisticsUpdatedMessage()
                );
            }
        }
    }
}
