using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using SCMM.Azure.ServiceBus.Extensions;

namespace SCMM.Azure.ServiceBus
{
    public class ServiceBusClient
    {
        private readonly global::Azure.Messaging.ServiceBus.ServiceBusClient _client;
        private readonly global::Azure.Messaging.ServiceBus.Administration.ServiceBusAdministrationClient _administrationClient;

        public ServiceBusClient(global::Azure.Messaging.ServiceBus.ServiceBusClient client, global::Azure.Messaging.ServiceBus.Administration.ServiceBusAdministrationClient administrationClient)
        {
            _client = client;
            _administrationClient = administrationClient;
        }

        public async Task SendMessageAsync<T>(T message, CancellationToken cancellationToken = default) where T : class, IMessage
        {
            await using var sender = _client.CreateSender<T>();
            await sender.SendMessageAsync(
                new ServiceBusJsonMessage<T>(message),
                cancellationToken
            );
        }

        public async Task SendMessagesAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default) where T : class, IMessage
        {
            await using var sender = _client.CreateSender<T>();
            using var batch = await sender.CreateMessageBatchAsync(cancellationToken);
            foreach (var message in messages)
            {
                batch.TryAddMessage(
                    new ServiceBusJsonMessage<T>(message)
                );
            }

            await sender.SendMessagesAsync(batch, cancellationToken);
        }

        public async Task<TResponse> SendMessageAndAwaitReplyAsync<TRequest, TResponse>(TRequest message, CancellationToken cancellationToken = default)
            where TRequest : class, IMessage
            where TResponse : class, IMessage
        {
            var correlationId = Guid.NewGuid();
            var replyToQueueName = $"reply-to-{correlationId}";
            var messageTimeout = TimeSpan.FromMinutes(5); // minimum allowed time for AutoDeleteOnIdle is 5 minutes

            try
            {
                await _administrationClient.CreateQueueAsync(
                    new CreateQueueOptions(replyToQueueName)
                    {
                        AutoDeleteOnIdle = (messageTimeout * 2)
                    },
                    cancellationToken
                );

                var requestClient = _client.CreateSender<TRequest>();
                var receiverClient = _client.CreateReceiver(
                    replyToQueueName,
                    new ServiceBusReceiverOptions()
                    {
                        ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete,
                    }
                );

                await requestClient.SendMessageAsync(
                    new ServiceBusJsonMessage<TRequest>(message)
                    {
                        ReplyTo = replyToQueueName
                    },
                    cancellationToken
                );

                var reply = await receiverClient.ReceiveMessageAsync(messageTimeout, cancellationToken);
                return reply?.Body?.ToObjectFromJson<TResponse>();
            }
            finally
            {
                if (await _administrationClient.QueueExistsAsync(replyToQueueName))
                {
                    await _administrationClient.DeleteQueueAsync(replyToQueueName);
                }
            }
        }
    }
}
