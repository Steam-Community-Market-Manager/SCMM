﻿using Microsoft.Extensions.Logging;
using SCMM.Shared.Data.Models.Extensions;
using System.Net;

namespace SCMM.Shared.Client;

public class RotatingWebProxy : IRotatingWebProxy, ICredentials, ICredentialsByHost
{
    private readonly ILogger<RotatingWebProxy> _logger;
    private WebProxyWithCooldown[] _proxies;

    public RotatingWebProxy(ILogger<RotatingWebProxy> logger, IEnumerable<WebProxyEndpoint> webProxyEndpoints)
    {
        var proxies = new List<WebProxyWithCooldown>()
        {
            new WebProxyWithCooldown() // self, no proxy, highest priority
        };

        if (webProxyEndpoints != null)
        {
            proxies.AddRange(webProxyEndpoints
                .Select(x => new WebProxyWithCooldown()
                {
                    Priority = webProxyEndpoints.ToList().IndexOf(x) + 1,
                    Address = new Uri(x.Url),
                    Credentials = x.Domain == null && x.Username == null && x.Password == null ? null : new NetworkCredential()
                    {
                        Domain = x.Domain,
                        UserName = x.Username,
                        Password = x.Password
                    },
                    IsEnabled = x.IsEnabled
                })
            );
        }

        _logger = logger;
        _proxies = proxies.ToArray();
    }

    private WebProxyWithCooldown GetNextAvailableProxy(Uri address)
    {
        var now = DateTime.UtcNow;
        var enabledProxies = _proxies.Where(x => x.IsEnabled);

        // Use the highest priority proxy that isn't in cooldown
        var proxy = enabledProxies
            .Where(x => x.GetHostCooldown(address) <= now)
            .OrderBy(x => x.Priority)
            .FirstOrDefault();

        if (proxy == null && enabledProxies.Any())
        {
            // Crap...
            _logger.LogWarning($"All available proxies for '{address?.Host}' are currently in cooldown! Request will by-pass the proxy.");
        }

        return proxy;
    }

    public void RotateProxy(Uri address, TimeSpan cooldown)
    {
        var proxy = GetNextAvailableProxy(address);
        if (proxy != null)
        {
            proxy.IncrementHostCooldown(address, cooldown);
            var newProxy = GetNextAvailableProxy(address);
            _logger.LogWarning($"'{address?.Host}' has entered a {cooldown.TotalSeconds}s cooldown on '{proxy?.Address?.Host ?? "default"}' proxy. Requests will now rotate to '{newProxy?.Address?.Host ?? "default"}' proxy.");
        }
    }

    public void DisableProxy(Uri address)
    {
        var proxy = GetNextAvailableProxy(address);
        if (proxy != null)
        {
            proxy.IsEnabled = false;
            _logger.LogWarning($"'{proxy?.Address?.Host ?? "default"}' proxy has been disabled.");
        }
    }

    Uri IWebProxy.GetProxy(Uri destination)
    {
        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        var proxy = GetNextAvailableProxy(destination);
        _logger.LogDebug($"'{destination}' is being routed through '{proxy?.Address?.Host ?? "default"}' proxy.");
        return proxy?.Address;
    }

    bool IWebProxy.IsBypassed(Uri host)
    {
        if (host == null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        var proxy = GetNextAvailableProxy(host);
        return proxy?.Address == null;
    }

    NetworkCredential ICredentials.GetCredential(Uri uri, string authType)
    {
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        var proxy = _proxies.FirstOrDefault(x => x.IsEnabled && x.Address == uri);
        return proxy?.Credentials;
    }

    NetworkCredential ICredentialsByHost.GetCredential(string host, int port, string authenticationType)
    {
        if (host == null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        var proxy = _proxies.FirstOrDefault(x => x.IsEnabled && x.Address?.Host == host && x.Address?.Port == port);
        return proxy?.Credentials;
    }

    ICredentials IWebProxy.Credentials
    {
        get => this;
        set => throw new NotImplementedException();
    }

    private class WebProxyWithCooldown
    {
        /// <summary>
        /// Lower the better
        /// </summary>
        public int Priority { get; set; }

        public Uri Address { get; set; }

        public NetworkCredential Credentials { get; set; }

        public bool IsEnabled { get; set; } = true;

        public IDictionary<string, DateTime> Cooldowns { get; private set; } = new Dictionary<string, DateTime>();

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