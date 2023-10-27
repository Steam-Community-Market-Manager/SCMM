﻿using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.WebProxies;
using SCMM.Shared.Data.Models.Extensions;
using System.Net;

namespace SCMM.Shared.Client;

public class RotatingWebProxy : IWebProxyManager, IWebProxy, ICredentials, ICredentialsByHost
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
        _webProxyRefreshTimer = new Timer(
            (x) => Task.Run(async () => await RefreshProxiesAsync()), 
            null, 
            TimeSpan.Zero, 
            TimeSpan.FromMinutes(WebProxyRefreshIntervalMinutes)
        );
    }

    public async Task RefreshProxiesAsync()
    {
        var endpoints = await _webProxyStatisticsService.GetAllStatisticsAsync();
        if (endpoints != null)
        {
            lock (_proxies)
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
        }
    }

    public int GetAvailableProxyCount(Uri host = null)
    {
        lock (_proxies)
        {
            var now = DateTime.UtcNow;
            return _proxies
                ?.Where(x => x.IsAvailable)
                ?.Where(x => host == null || x.GetHostCooldown(host) <= now)
                ?.Count() ?? 0;
        }
    }

    public string GetProxyId(Uri requestAddress)
    {
        lock(_proxies)
        {
            return _proxies?.FirstOrDefault(x => x.CurrentRequestAddress == requestAddress)?.Id;
        }
    }

    public void UpdateProxyRequestStatistics(string proxyId, Uri requestAddress, HttpStatusCode responseStatusCode)
    {
        WebProxyWithCooldown proxy = null;
        lock (_proxies)
        {
            proxy = _proxies?.FirstOrDefault(x => x.Id == proxyId);
        }

        if (proxy != null && !String.IsNullOrEmpty(proxy.Address.ToString()))
        {
            var lastAccessedOn = DateTimeOffset.Now;
            _logger.LogDebug($"Proxy '{proxyId}' response was {responseStatusCode} for '{proxy.CurrentRequestAddress}'.");

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

    public void CooldownProxy(string proxyId, Uri host, TimeSpan cooldown)
    {
        WebProxyWithCooldown proxy = null;
        lock (_proxies)
        {
            proxy = _proxies?.FirstOrDefault(x => x.Id == proxyId);
        }

        if (proxy != null && !String.IsNullOrEmpty(proxy.Address.ToString()))
        {
            proxy.IncrementHostCooldown(host, cooldown);
            _logger.LogDebug($"Proxy '{proxyId}' incurred a {cooldown.TotalSeconds}s cooldown for '{host?.Host}'.");

            _webProxyStatisticsService.UpdateStatisticsAsync(proxy.Address.ToString(), (value) =>
            {
                value.DomainRateLimits ??= new Dictionary<string, DateTimeOffset>();
                value.DomainRateLimits[host.Host] = proxy.GetHostCooldown(host);
            });
        }
    }

    public void DisableProxy(string proxyId)
    {
        WebProxyWithCooldown proxy = null;
        lock (_proxies)
        {
            proxy = _proxies?.FirstOrDefault(x => x.Id == proxyId);
        }

        if (proxy != null && !String.IsNullOrEmpty(proxy.Address.ToString()))
        {
            proxy.IsEnabled = false;
            _logger.LogDebug($"Proxy '{proxyId}' has been disabled.");

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

        lock(_proxies)
        {
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
    }

    bool IWebProxy.IsBypassed(Uri host)
    {
        if (host == null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        var proxiesAreAvailable = (GetAvailableProxyCount(host) > 0);
        if (!proxiesAreAvailable)
        {
            _logger.LogWarning($"There are no available proxies to handle new requests to '{host.Host}'. The request will bypass all configured proxies.");
        }

        return !proxiesAreAvailable;
    }

    NetworkCredential ICredentials.GetCredential(Uri uri, string authType)
    {
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        lock (_proxies)
        {
            return _proxies
                ?.FirstOrDefault(x => x.IsEnabled && x.Address == uri)
                ?.Credentials;
        }
    }

    NetworkCredential ICredentialsByHost.GetCredential(string host, int port, string authenticationType)
    {
        if (host == null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        lock (_proxies)
        {
            return _proxies
                ?.FirstOrDefault(x => x.IsEnabled && x.Address?.Host == host && x.Address?.Port == port)
                ?.Credentials;
        }
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
        public Uri CurrentRequestAddress { get; set; }

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
