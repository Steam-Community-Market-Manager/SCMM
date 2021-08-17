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

        public async Task SendMessageAsync<T>(T message, CancellationToken cancellationToken = default(CancellationToken)) where T : class, IMessage
        {
            await using var sender = _client.CreateSender<T>();
            await sender.SendMessageAsync(
                new ServiceBusMessage(BinaryData.FromObjectAsJson(message)),
                cancellationToken
            );
        }

        public async Task SendMessagesAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default(CancellationToken)) where T : class, IMessage
        {
            await using var messageSender = _client.CreateSender<T>();
            using var messageBatch = await messageSender.CreateMessageBatchAsync(cancellationToken);
            foreach (var message in messages)
            {
                messageBatch.TryAddMessage(
                    new ServiceBusMessage(BinaryData.FromObjectAsJson(message))
                );
            }

            await messageSender.SendMessagesAsync(messageBatch, cancellationToken);
        }

        public async Task<TResponse> SendMessageAndAwaitReplyAsync<TRequest, TResponse>(TRequest message, CancellationToken cancellationToken = default(CancellationToken))
            where TRequest : class, IMessage
            where TResponse : class, IMessage
        {
            var temporaryQueueName = Guid.NewGuid().ToString();
            var messageTimeout = TimeSpan.FromMinutes(5); // minimum allowed time for AutoDeleteOnIdle is 5 minutes

            try
            {
                await _administrationClient.CreateQueueAsync(
                    new CreateQueueOptions(temporaryQueueName)
                    {
                        AutoDeleteOnIdle = (messageTimeout * 2)
                    },
                    cancellationToken
                );

                var requestClient = _client.CreateSender<TRequest>();
                var receiverClient = _client.CreateReceiver(
                    temporaryQueueName,
                    new ServiceBusReceiverOptions()
                    {
                        ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
                    }
                );

                await requestClient.SendMessageAsync(
                    new ServiceBusMessage(BinaryData.FromObjectAsJson(message))
                    {
                        ReplyTo = temporaryQueueName
                    },
                    cancellationToken
                );

                var reply = await receiverClient.ReceiveMessageAsync(messageTimeout, cancellationToken);
                return reply?.Body?.ToObjectFromJson<TResponse>();
            }
            finally
            {
                if (await _administrationClient.QueueExistsAsync(temporaryQueueName))
                {
                    await _administrationClient.DeleteQueueAsync(temporaryQueueName);
                }
            }
        }
    }
}
