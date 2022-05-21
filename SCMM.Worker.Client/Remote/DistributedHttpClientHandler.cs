using SCMM.Azure.ServiceBus;
using System.Net;

namespace SCMM.Worker.Client.Remote;

public class DistributedHttpClientHandler : HttpMessageHandler
{
    private readonly ServiceBusClient _serviceBusClient;

    public DistributedHttpClientHandler(ServiceBusClient serviceBusClient)
    {
        _serviceBusClient = serviceBusClient;
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Distributed http client must be used asynchronously only");
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_serviceBusClient == null)
        {
            return null;
        }

        var remoteResponse = await _serviceBusClient.SendMessageAndAwaitReplyAsync<RemoteHttpRequestMessage, RemoteHttpResponseMessage>(
            new RemoteHttpRequestMessage()
            {
                Headers = request.Headers?.ToDictionary(x => x.Key, x => x.Value.ToArray()),
                ContentHeaders = request.Content?.Headers?.ToDictionary(x => x.Key, x => x.Value.ToArray()),
                Method = request.Method.ToString(),
                Content = request.Content != null ? await request.Content?.ReadAsByteArrayAsync() : null,
                Options = request.Options,
                RequestUri = request.RequestUri,
                Version = request.Version.ToString(),
                VersionPolicy = request.VersionPolicy,
                Cookies = UseCookies && CookieContainer != null ? CookieContainer.GetAllCookies() : null
            },
            cancellationToken
        );

        var content = new ByteArrayContent(remoteResponse.Content);
        if (remoteResponse.ContentHeaders != null)
        {
            foreach (var header in remoteResponse.ContentHeaders)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        var response = new HttpResponseMessage()
        {
            RequestMessage = request,
            StatusCode = remoteResponse.StatusCode,
            ReasonPhrase = remoteResponse.ReasonPhrase,
            Content = content,
            Version = Version.Parse(remoteResponse.Version)
        };

        if (remoteResponse.Headers != null)
        {
            foreach (var remoteResponseHeader in remoteResponse.Headers)
            {
                response.Headers.TryAddWithoutValidation(remoteResponseHeader.Key, remoteResponseHeader.Value.ToArray());
            }
        }

        return response;
    }

    public bool UseCookies { get; set; }

    public CookieContainer CookieContainer { get; set; }
}
