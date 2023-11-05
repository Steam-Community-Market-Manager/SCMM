using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.WebProxies;
using SCMM.Shared.Data.Models.Extensions;
using System.Net;

namespace SCMM.Shared.Web.Client;

public class RotatingWebProxy : IWebProxyManager, IWebProxy, ICredentials, ICredentialsByHost, IDisposable
{
    private const int WebProxySyncIntervalMinutes = 3;

    private readonly ILogger<RotatingWebProxy> _logger;
    private readonly IWebProxyUsageStatisticsService _webProxyStatisticsService;
    private readonly Timer _webProxySyncTimer;

    private IList<WebProxyWithCooldown> _proxies = new List<WebProxyWithCooldown>();

    [ThreadStatic]
    private static string currentThreadReservedWebProxyId;

    public RotatingWebProxy(ILogger<RotatingWebProxy> logger, IWebProxyUsageStatisticsService webProxyStatisticsService)
    {
        _logger = logger;
        _webProxyStatisticsService = webProxyStatisticsService;
        _webProxySyncTimer = new Timer(
            (x) => Task.Run(async () => await RefreshProxiesAsync()),
            null,
            TimeSpan.FromMinutes(WebProxySyncIntervalMinutes),
            TimeSpan.FromMinutes(WebProxySyncIntervalMinutes)
        );
    }

    public void Dispose()
    {
        _webProxySyncTimer?.Dispose();
    }

    public async Task RefreshProxiesAsync()
    {
        // TODO: Web proxy details should be stored in a proper database (CosmosDB?), not the usage statistics cache.
        //       It's not a good idea to store the proxy ip/username/password here, but it is just too convenient having everything in one place that is fast [like Redis]. 

        var endpoints = await _webProxyStatisticsService.GetAsync();
        if (endpoints != null)
        {
            lock (_proxies)
            {
                // Remove old proxies
                _proxies.RemoveAll(x => !endpoints.Any(y => y.Id == x.Id));

                // Update existing proxies
                var proxiesToUpdate = _proxies.Join(endpoints, x => x.Id, y => y.Id, (Proxy, Endpoint) => new { Proxy, Endpoint });
                foreach (var x in proxiesToUpdate)
                {
                    x.Proxy.Cooldowns = x.Endpoint.DomainRateLimits?.ToDictionary(k => k.Key, v => v.Value.UtcDateTime) ?? new Dictionary<string, DateTime>();
                    x.Proxy.LastAccessedOn = x.Endpoint.LastAccessedOn;
                    x.Proxy.IsAvailable = x.Endpoint.IsAvailable;
                };

                // Add new proxies
                _proxies.AddRange(endpoints
                    .Where(x => !_proxies.Any(y => y.Id == x.Id))
                    .Where(x => !string.IsNullOrEmpty(x.Address) && x.Port > 0)
                    .OrderBy(x => x.Id)
                    .Select(x => new WebProxyWithCooldown()
                    {
                        Id = x.Id,
                        Address = new Uri(x.Url),
                        Credentials = x.Username == null && x.Password == null ? null : new NetworkCredential()
                        {
                            UserName = x.Username,
                            Password = x.Password
                        },
                        Cooldowns = x.DomainRateLimits?.ToDictionary(k => k.Key, v => v.Value.UtcDateTime) ?? new Dictionary<string, DateTime>(),
                        LastAccessedOn = x.LastAccessedOn,
                        IsAvailable = x.IsAvailable,
                    })
                );
            }
        }
    }

    public int GetAvailableProxyCount(Uri host = null)
    {
        var now = DateTime.UtcNow;
        return _proxies
            ?.Where(x => x.IsAvailable)
            ?.Where(x => host == null || x.GetHostCooldown(host) <= now)
            ?.Count() ?? 0;
    }

    public void CooldownProxy(string proxyId, Uri host, TimeSpan cooldown)
    {
        var proxy = _proxies.FirstOrDefault(x => x.Id == proxyId);
        if (proxy != null && !string.IsNullOrEmpty(proxy.Address.ToString()))
        {
            proxy.IncrementHostCooldown(host, cooldown);
            _logger.LogDebug($"Proxy '{proxyId}' incurred a {cooldown.TotalSeconds}s cooldown for '{host?.Host}'.");

            _webProxyStatisticsService.PatchAsync(proxy.Address.ToString(), (value) =>
            {
                value.DomainRateLimits ??= new Dictionary<string, DateTimeOffset>();
                value.DomainRateLimits[host.Host] = proxy.GetHostCooldown(host);
            });
        }
    }

    public void DisableProxy(string proxyId)
    {
        var proxy = _proxies.FirstOrDefault(x => x.Id == proxyId);
        if (proxy != null && !string.IsNullOrEmpty(proxy.Address.ToString()))
        {
            proxy.IsAvailable = false;
            _logger.LogDebug($"Proxy '{proxyId}' has been disabled.");

            _webProxyStatisticsService.PatchAsync(proxy.Address.ToString(), (value) =>
            {
                value.IsAvailable = false;
            });
        }
    }

    public void UpdateProxyRequestStatistics(string proxyId, Uri requestAddress, HttpStatusCode? responseStatusCode = null)
    {
        var proxy = _proxies.FirstOrDefault(x => x.Id == proxyId);
        if (proxy != null && !string.IsNullOrEmpty(proxy.Address.ToString()))
        {
            var lastAccessedOn = DateTimeOffset.Now;
            proxy.LastAccessedOn = lastAccessedOn;
            _logger.LogDebug($"Proxy '{proxyId}' response was {(responseStatusCode?.ToString() ?? "unknown")} for '{(requestAddress?.ToString() ?? "unknown")}'.");

            _webProxyStatisticsService.PatchAsync(proxy.Address.ToString(), (value) =>
            {
                value.LastAccessedOn = lastAccessedOn;
                if (responseStatusCode >= HttpStatusCode.OK && responseStatusCode < HttpStatusCode.Ambiguous)
                {
                    value.RequestsSucceededCount++; // It was a successful request
                }
                else
                {
                    value.RequestsFailedCount++; // It was an unsuccessful request
                }
            });
        }
    }

    Uri IWebProxy.GetProxy(Uri destination)
    {
        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        // Find the least recently accessed proxy that isn't in cooldown for our domain
        var now = DateTime.UtcNow;
        var availableProxies = _proxies
            .Where(x => x.IsAvailable)
            .Where(x => x.GetHostCooldown(destination) <= now);
        var proxy = availableProxies
            .OrderBy(x => x.LastAccessedOn)
            .FirstOrDefault();

        // If a proxy is available, associate it with our thread
        if (proxy != null)
        {
            // NOTE: We update the last access time so this proxy drops to the bottom of the pick list for the next request
            proxy.LastAccessedOn = DateTimeOffset.Now;
            currentThreadReservedWebProxyId = proxy.Id;
        }

        _logger.LogDebug($"'{destination}' is being routed through '{proxy?.Address?.Host ?? "default"}' proxy. (total: {_proxies.Count}, available: {availableProxies.Count()}, host: '{destination.Host}')");
        return proxy?.Address;
    }

    bool IWebProxy.IsBypassed(Uri host)
    {
        if (host == null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        var proxiesAreAvailable = GetAvailableProxyCount(host) > 0;
        var proxyIsAlreadyAllocatedToThread = !string.IsNullOrEmpty(currentThreadReservedWebProxyId);
        var proxyIsBypassed = (!proxiesAreAvailable && !proxyIsAlreadyAllocatedToThread);
        if (proxyIsBypassed)
        {
            _logger.LogError($"There are no available proxies to handle new requests to '{host.Host}' (total: {_proxies.Count}, available: 0). The request will bypass configured web proxy settings.");
        }

        return proxyIsBypassed;
    }

    NetworkCredential ICredentials.GetCredential(Uri uri, string authType)
    {
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        return _proxies
            ?.FirstOrDefault(x => x.IsAvailable && x.Address == uri)
            ?.Credentials;
    }

    NetworkCredential ICredentialsByHost.GetCredential(string host, int port, string authenticationType)
    {
        if (host == null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        return _proxies
            ?.FirstOrDefault(x => x.IsAvailable && x.Address?.Host == host && x.Address?.Port == port)
            ?.Credentials;
    }

    ICredentials IWebProxy.Credentials
    {
        get => this;
        set => throw new NotImplementedException();
    }

    public string CurrentProxyId => currentThreadReservedWebProxyId;

    private class WebProxyWithCooldown
    {
        /// <summary>
        /// Unique id of the proxy
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Proxy address
        /// </summary>
        public Uri Address { get; set; }

        /// <summary>
        /// Proxy credentials
        /// </summary>
        public NetworkCredential Credentials { get; set; }

        /// <summary>
        /// Rate-limit cooldown time for each domain
        /// </summary>
        public IDictionary<string, DateTime> Cooldowns { get; internal set; } = new Dictionary<string, DateTime>();

        /// <summary>
        /// Last time this proxy was used
        /// </summary>
        public DateTimeOffset? LastAccessedOn { get; set; }

        /// <summary>
        /// True if this proxy is available for us
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        public DateTime GetHostCooldown(Uri address)
        {
            return Cooldowns.GetOrDefault(address?.Host ?? string.Empty, DateTime.MinValue);
        }

        public void IncrementHostCooldown(Uri address, TimeSpan cooldownPeriod)
        {
            var now = DateTime.UtcNow;
            var host = address?.Host ?? string.Empty;
            var cooldown = Cooldowns.GetOrDefault(host, DateTime.MinValue);

            // If the last cooldown was in the past, bump it to the current date/time
            if (cooldown < now)
            {
                cooldown = now;
            }

            // Increment the cooldown to AT LEAST "current time + cooldown period"
            if (cooldown < (now + cooldownPeriod))
            {
                Cooldowns[host] = (now + cooldownPeriod);
            }
        }
    }
}
