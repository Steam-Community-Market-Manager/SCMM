using SCMM.Azure.ServiceBus;
using System.Net;

namespace SCMM.Worker.Client.Remote;

public class RemoteHttpResponseMessage : IMessage
{
    public IDictionary<string, string[]> Headers { get; set; }

    public IDictionary<string, string[]> ContentHeaders { get; set; }

    public HttpStatusCode StatusCode { get; set; }

    public string ReasonPhrase { get; set; }

    public byte[] Content { get; set; }

    public string Version { get; set; }
}
