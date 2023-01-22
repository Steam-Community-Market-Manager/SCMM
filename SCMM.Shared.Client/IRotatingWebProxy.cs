using System.Net;

namespace SCMM.Shared.Client;

public interface IRotatingWebProxy : IWebProxy
{
    void UpdateRequestStatistics(Uri address, HttpStatusCode responseStatusCode);

    void RotateProxy(Uri address, TimeSpan cooldown);

    void DisableProxy(Uri address);
}
