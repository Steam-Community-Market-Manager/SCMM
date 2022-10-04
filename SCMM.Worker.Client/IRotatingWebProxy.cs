using System.Net;

namespace SCMM.Worker.Client;

public interface IRotatingWebProxy : IWebProxy
{
    void RotateProxy(Uri address, TimeSpan cooldown);
}
