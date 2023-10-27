using Microsoft.Azure.Functions.Worker;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.WebProxies;
using SCMM.Shared.API.Events;
using SCMM.Shared.Client;

namespace SCMM.Steam.Functions.Timer;

public class UpdateWebProxyList
{
    private readonly IWebProxyManagementService _webProxyManagementService;
    private readonly IWebProxyUsageStatisticsService _webProxyStatisticsService;
    private readonly IWebProxyManager _webProxyManager;
    private readonly IServiceBus _serviceBus;

    public UpdateWebProxyList(IWebProxyManagementService webProxyManagementService, IWebProxyUsageStatisticsService webProxyStatisticsService, IWebProxyManager webProxyManager, IServiceBus serviceBus)
    {
        _webProxyManagementService = webProxyManagementService;
        _webProxyStatisticsService = webProxyStatisticsService;
        _webProxyManager = webProxyManager;
        _serviceBus = serviceBus;
    }

    [Function("Update-Web-Proxy-List")]
    public async Task Run([TimerTrigger("0 0 * * * *")] /* every hour, on the hour */ TimerInfo timerInfo, FunctionContext context)
    {
        var logger = context.GetLogger("Update-Web-Proxy-List");
        var proxiesHaveChanged = false;

        // Get the latest list of proxies
        var webProxies = await _webProxyManagementService.ListWebProxiesAsync();
        if (webProxies != null)
        {
            // TODO: Web proxy details should be stored in a proper database (CosmosDB?), not the usage statistics cache.
            //       It's not a good idea to store the proxy ip/username/password here, but it is just too convenient having everything in one place that is fast [like Redis]. 

            // Get cached list
            var cachedWebProxies = new List<WebProxyWithUsageStatistics>(
                await _webProxyStatisticsService.GetAsync() ?? Enumerable.Empty<WebProxyWithUsageStatistics>()
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
                        cachedWebProxy = new WebProxyWithUsageStatistics()
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
            await _webProxyStatisticsService.SetAsync(
                cachedWebProxies.Where(x => !String.IsNullOrEmpty(x.Address) && x.Port > 0)
            );

            if (proxiesHaveChanged)
            {
                await _webProxyManager.RefreshProxiesAsync();
                await _serviceBus.SendMessageAsync(
                    new WebProxyStatisticsUpdatedMessage()
                );
            }
        }
    }
}
