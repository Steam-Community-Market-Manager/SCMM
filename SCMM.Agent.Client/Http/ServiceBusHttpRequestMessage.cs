using SCMM.Azure.ServiceBus.Attributes;

namespace SCMM.Azure.ServiceBus.Http;

[Queue(Name = "Http-Requests")]
public class ServiceBusHttpRequestMessage : IMessage
{
    public IDictionary<string, string[]> Headers { get; set; }

    public IDictionary<string, string[]> ContentHeaders { get; set; }

    public string Method { get; set; }

    public byte[] Content { get; set; }

    public HttpRequestOptions Options { get; set; }

    public Uri? RequestUri { get; set; }

    public string Version { get; set; }

    public HttpVersionPolicy VersionPolicy { get; set; }
}
