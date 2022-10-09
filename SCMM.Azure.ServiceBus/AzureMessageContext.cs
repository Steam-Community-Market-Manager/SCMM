using Azure.Messaging.ServiceBus;
using SCMM.Shared.Abstractions.Messaging;

namespace SCMM.Azure.ServiceBus
{
    public class AzureMessageContext : IMessageContext
    {
        private readonly ServiceBusClient _client;

        public AzureMessageContext(ServiceBusClient client)
        {
            _client = client;
        }

        public string MessageId { get; set; }

        public Type MessageType { get; set; }

        public string ReplyTo { get; set; }

        public async Task ReplyAsync<T>(T message, CancellationToken cancellationToken = default) where T : class, IMessage
        {
            if (string.IsNullOrEmpty(ReplyTo))
            {
                throw new ArgumentNullException(nameof(ReplyTo));
            }

            await using var sender = _client.CreateSender(ReplyTo);
            await sender.SendMessageAsync(
                new ServiceBusJsonMessage<T>(message)
                {
                    CorrelationId = MessageId,
                },
                cancellationToken
            );
        }
    }
}
