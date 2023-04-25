using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.WebProxies;
using SCMM.Shared.Data.Models.Extensions;
using System.Net;

namespace SCMM.Shared.Client;

public class RotatingWebProxy : IRotatingWebProxy, ICredentials, ICredentialsByHost
{
    private const int WebProxyRefreshIntervalMinutes = 10;

    private readonly ILogger<RotatingWebProxy> _logger;
    private readonly IWebProxyStatisticsService _webProxyStatisticsService;
    private readonly Timer _webProxyRefreshTimer;

    private WebProxyWithCooldown[] _proxies = new WebProxyWithCooldown[0];

    public RotatingWebProxy(ILogger<RotatingWebProxy> logger, IWebProxyStatisticsService webProxyStatisticsService)
    {
        _logger = logger;
        _webProxyStatisticsService = webProxyStatisticsService;
        _webProxyRefreshTimer = new Timer(RefreshProxies, null, TimeSpan.Zero, TimeSpan.FromMinutes(WebProxyRefreshIntervalMinutes));
    }

    private void RefreshProxies(object _)
    {
        Task.Run(async () =>
        {
            var endpoints = await _webProxyStatisticsService.GetAllStatisticsAsync();
            if (endpoints != null)
            {
                _proxies = endpoints
                    .Where(x => !String.IsNullOrEmpty(x.Address) && x.Port > 0)
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
                        IsEnabled = x.IsAvailable,
                    })
                    .ToArray();
            }
        });
    }

    public string GetProxyId(Uri requestAddress)
    {
        return _proxies?.FirstOrDefault(x => x.CurrentRequestAddress == requestAddress)?.Id;
    }

    public void UpdateProxyRequestStatistics(string proxyId, Uri address, HttpStatusCode responseStatusCode)
    {
        var proxy = _proxies?.FirstOrDefault(x => x.Id == proxyId);
        if (proxy != null)
        {
            var lastAccessedOn = DateTimeOffset.Now;
            _webProxyStatisticsService.UpdateStatisticsAsync(proxy.Address.ToString(), (value) =>
            {
                value.LastAccessedOn = lastAccessedOn;
                if (responseStatusCode >= HttpStatusCode.OK && responseStatusCode < HttpStatusCode.Ambiguous)
                {
                    value.RequestsSucceededCount++;
                }
                else
                {
                    value.RequestsFailedCount++;
                }
            }).ContinueWith((x) =>
            {
                proxy.LastAccessedOn = lastAccessedOn;
                proxy.CurrentRequestAddress = null;
            });
        }
    }

    public void RotateProxy(string proxyId, Uri host, TimeSpan cooldown)
    {
        var proxy = _proxies?.FirstOrDefault(x => x.Id == proxyId);
        if (proxy != null)
        {
            proxy.IncrementHostCooldown(host, cooldown);
            _logger.LogDebug($"'{host?.Host}' has entered a {cooldown.TotalSeconds}s cooldown on '{proxy?.Address?.Host ?? "default"}' proxy.");

            _webProxyStatisticsService.UpdateStatisticsAsync(proxy.Address.ToString(), (value) =>
            {
                value.DomainRateLimits ??= new Dictionary<string, DateTimeOffset>();
                value.DomainRateLimits[host.Host] = proxy.GetHostCooldown(host);
            });
        }
    }

    public void DisableProxy(string proxyId)
    {
        var proxy = _proxies?.FirstOrDefault(x => x.Id == proxyId);
        if (proxy != null)
        {
            proxy.IsEnabled = false;
            _logger.LogDebug($"'{proxy?.Address?.Host ?? "default"}' proxy has been disabled.");

            _webProxyStatisticsService.UpdateStatisticsAsync(proxy.Address.ToString(), (value) =>
            {
                value.IsAvailable = false;
            });
        }
    }

    Uri IWebProxy.GetProxy(Uri destination)
    {
        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        // Use the least recently accessed proxy that isn't in cooldown for our domain
        var now = DateTime.UtcNow;
        var proxy = _proxies
            ?.Where(x => x.IsAvailable)
            ?.Where(x => x.GetHostCooldown(destination) <= now)
            ?.OrderBy(x => x.LastAccessedOn)
            ?.FirstOrDefault();

        if (proxy != null)
        {
            // Mark this proxy as busy
            proxy.CurrentRequestAddress = destination;
        }

        _logger.LogDebug($"'{destination}' is being routed through '{proxy?.Address?.Host ?? "default"}' proxy.");
        return proxy?.Address;
    }

    bool IWebProxy.IsBypassed(Uri host)
    {
        if (host == null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        var now = DateTime.UtcNow;
        return _proxies
            ?.Where(x => x.IsAvailable)
            ?.Where(x => x.GetHostCooldown(host) <= now)
            ?.FirstOrDefault() == null;
    }

    NetworkCredential ICredentials.GetCredential(Uri uri, string authType)
    {
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        return _proxies
            ?.FirstOrDefault(x => x.IsEnabled && x.Address == uri)
            ?.Credentials;
    }

    NetworkCredential ICredentialsByHost.GetCredential(string host, int port, string authenticationType)
    {
        if (host == null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        return _proxies
            ?.FirstOrDefault(x => x.IsEnabled && x.Address?.Host == host && x.Address?.Port == port)
            ?.Credentials;
    }

    ICredentials IWebProxy.Credentials
    {
        get => this;
        set => throw new NotImplementedException();
    }

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
        /// The current address this proxy is serving
        /// </summary>
        public Uri? CurrentRequestAddress { get; set; }

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
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// True if this proxy is currently busy serving a request
        /// </summary>
        public bool IsBusy => (CurrentRequestAddress != null);

        /// <summary>
        /// True if this proxy is available to serve a new request
        /// </summary>
        public bool IsAvailable => (IsEnabled && !IsBusy);

        public DateTime GetHostCooldown(Uri address)
        {
            return Cooldowns.GetOrDefault(address?.Host ?? string.Empty, DateTime.MinValue);
        }

        public void IncrementHostCooldown(Uri address, TimeSpan increment)
        {
            var now = DateTime.UtcNow;
            var host = address?.Host ?? string.Empty;
            var cooldown = Cooldowns.GetOrDefault(host, DateTime.MinValue);
            if (cooldown < now)
            {
                // If the last cooldown was in the past, bump it to the current date/time
                cooldown = now;
            }

            // cooldown += increment
            Cooldowns[host] = cooldown.Add(increment);
        }
    }
}
