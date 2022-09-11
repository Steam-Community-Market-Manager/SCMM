using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;
using System.Net;

namespace SCMM.Worker.Client.Remote;

[Queue(Name = "Remote-Http-Requests")]
public class RemoteHttpRequestMessage : Message
{
    public IDictionary<string, string[]> Headers { get; set; }

    public IDictionary<string, string[]> ContentHeaders { get; set; }

    public string Method { get; set; }

    public byte[] Content { get; set; }

    public HttpRequestOptions Options { get; set; }

    public Uri RequestUri { get; set; }

    public string Version { get; set; }

    public HttpVersionPolicy VersionPolicy { get; set; }

    public IEnumerable<Cookie> Cookies { get; set; }
}
