using System.Net;

namespace SCMM.Shared.Web.Client;

public interface IWebProxyManager : IWebProxy
{
    Task RefreshProxiesAsync();

    int GetAvailableProxyCount(Uri host);

    void CooldownProxy(string proxyId, Uri host, TimeSpan cooldown);

    void DisableProxy(string proxyId);

    void UpdateProxyRequestStatistics(string proxyId, Uri requestAddress, HttpStatusCode? responseStatusCode = null);

    /// <summary>
    /// Gets the web proxy id assigned to the current thread (if any).
    /// Used post-request to follow up with proxy actions (e.g. cooldown, disable, update stats)
    /// </summary>
    string LastProxyId { get; }
}
