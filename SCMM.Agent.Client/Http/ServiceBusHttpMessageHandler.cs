namespace SCMM.Azure.ServiceBus.Http;

public class ServiceBusHttpMessageHandler : HttpMessageHandler
{
    private readonly ServiceBusClient _serviceBusClient;

    public ServiceBusHttpMessageHandler(ServiceBusClient serviceBusClient)
    {
        _serviceBusClient = serviceBusClient;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var remoteResponse = await _serviceBusClient.SendMessageAndAwaitReplyAsync<ServiceBusHttpRequestMessage, ServiceBusHttpResponseMessage>(
            new ServiceBusHttpRequestMessage()
            {
                Headers = request.Headers?.ToDictionary(x => x.Key, x => x.Value.ToArray()),
                ContentHeaders = request.Content?.Headers?.ToDictionary(x => x.Key, x => x.Value.ToArray()),
                Method = request.Method.ToString(),
                Content = (request.Content != null ? await request.Content?.ReadAsByteArrayAsync() : null),
                Options = request.Options,
                RequestUri = request.RequestUri,
                Version = request.Version.ToString(),
                VersionPolicy = request.VersionPolicy
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
}
