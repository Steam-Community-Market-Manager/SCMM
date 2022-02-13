using SCMM.Azure.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;
using SCMM.Azure.ServiceBus.Http;
using SCMM.Market.Client;

namespace SCMM.Agent.Server.Handlers
{
    [Concurrency(MaxConcurrentCalls = 1)]
    public class ServiceBusHttpRequestMessageHandler : AgentWebClient, IMessageHandler<ServiceBusHttpRequestMessage>
    {
        public async Task HandleAsync(ServiceBusHttpRequestMessage message, MessageContext context)
        {
            using (var client = BuildHttpClient())
            {
                var content = (HttpContent)null;
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
                        message.Headers.TryAdd(header.Key, header.Value);
                    }
                }
                if (message.Options != null)
                {
                    foreach (var option in message.Options)
                    {
                        message.Options.TryAdd(option.Key, option.Value);
                    }
                }

                var response = await client.SendAsync(request);
                var binaryContent = await response.Content.ReadAsByteArrayAsync();
                await context.ReplyAsync(new ServiceBusHttpResponseMessage()
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
