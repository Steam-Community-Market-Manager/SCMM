using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;
using SCMM.Worker.Client.Remote;

namespace SCMM.Worker.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 1)]
    public class RemoteHttpRequestMessageHandler : Worker.Client.WebClient, IMessageHandler<RemoteHttpRequestMessage>
    {
        public async Task HandleAsync(RemoteHttpRequestMessage message, MessageContext context)
        {
            using (var client = BuildHttpClient())
            {
                var content = (HttpContent?) null;
                if (message.Content != null)
                {
                    content = new ByteArrayContent(message.Content);
                    if (message.ContentHeaders != null)
                    {
                        foreach (var header in message.ContentHeaders)
                        {
                            content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                }

                var request = new HttpRequestMessage()
                {
                    Method = new HttpMethod(message.Method),
                    Content = content,
                    RequestUri = message.RequestUri,
                    Version = Version.Parse(message.Version),
                    VersionPolicy = message.VersionPolicy
                };

                if (message.Headers != null)
                {
                    foreach (var header in message.Headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }

                if (message.Cookies != null)
                {
                    request.Headers.Add(
                        "Cookie",
                        message.Cookies.Select(x => $"{Uri.EscapeDataString(x.Name)}={Uri.EscapeDataString(x.Value)}").ToArray()
                    );
                }

                if (message.Options != null)
                {
                    foreach (var option in message.Options)
                    {
                        request.Options.TryAdd(option.Key, option.Value);
                    }
                }

                var response = await client.SendAsync(request);
                var binaryContent = await response.Content.ReadAsByteArrayAsync();
                await context.ReplyAsync(new RemoteHttpResponseMessage()
                {
                    Headers = response.Headers?.ToDictionary(x => x.Key, x => x.Value.ToArray()),
                    StatusCode = response.StatusCode,
                    ReasonPhrase = response.ReasonPhrase,
                    Content = binaryContent,
                    Version = response.Version.ToString()
                });
            }
        }
    }
}
