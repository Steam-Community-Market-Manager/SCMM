using System.Net;

namespace SCMM.Shared.Client;

public interface IRotatingWebProxy : IWebProxy
{
    string GetProxyId(Uri requestAddress);

    void UpdateProxyRequestStatistics(string proxyId, Uri requestAddress, HttpStatusCode responseStatusCode);

    void CooldownProxy(string proxyId, Uri host, TimeSpan cooldown);

    void DisableProxy(string proxyId);
}
