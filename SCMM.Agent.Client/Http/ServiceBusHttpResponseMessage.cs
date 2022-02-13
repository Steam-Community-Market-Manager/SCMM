using System.Net;

namespace SCMM.Azure.ServiceBus.Http;

public class ServiceBusHttpResponseMessage : IMessage
{
    public IDictionary<string, string[]> Headers { get; set; }

    public IDictionary<string, string[]> ContentHeaders { get; set; }

    public HttpStatusCode StatusCode { get; set; }

    public string? ReasonPhrase { get; set; }

    public byte[] Content { get; set; }

    public string Version { get; set; }
}
