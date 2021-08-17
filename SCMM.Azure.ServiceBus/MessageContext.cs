using Azure.Messaging.ServiceBus;

namespace SCMM.Azure.ServiceBus
{
    public class MessageContext
    {
        private readonly global::Azure.Messaging.ServiceBus.ServiceBusClient _client;

        public MessageContext(global::Azure.Messaging.ServiceBus.ServiceBusClient client)
        {
            _client = client;
        }

        public string MessageId { get; set; }

        public string ReplyTo { get; set; }

        public async Task ReplyAsync<T>(T message, CancellationToken cancellationToken = default(CancellationToken)) where T : class, IMessage
        {
            if (string.IsNullOrEmpty(ReplyTo))
            {
                throw new ArgumentNullException(nameof(ReplyTo));
            }

            await using var sender = _client.CreateSender(ReplyTo);
            await sender.SendMessageAsync(
                new ServiceBusMessage(BinaryData.FromObjectAsJson(message)),
                cancellationToken
            );
        }
    }
}
