using System.Net;

namespace SCMM.Shared.Web.Client;

public interface IWebProxyManager : IWebProxy
{
    Task RefreshProxiesAsync();

    int GetAvailableProxyCount(Uri host);

    string GetProxyId(Uri requestAddress);

    void UpdateProxyRequestStatistics(string proxyId, Uri requestAddress, HttpStatusCode responseStatusCode);

    void CooldownProxy(string proxyId, Uri host, TimeSpan cooldown);

    void DisableProxy(string proxyId);
}
