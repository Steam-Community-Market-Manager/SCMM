﻿using Azure.Messaging.ServiceBus;
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

        public async Task ScheduleMessageAsync<T>(T message, DateTimeOffset scheduledEnqueueTime, CancellationToken cancellationToken = default) where T : class, IMessage
        {
            await using var sender = _client.CreateSender<T>();
            await sender.ScheduleMessageAsync(
                new ServiceBusJsonMessage<T>(message),
                scheduledEnqueueTime,
                cancellationToken
            );
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

        public async Task<TResponse> SendMessageAndAwaitReplyAsync<TRequest, TResponse>(TRequest message, int maxTimeToWaitSeconds = 30, CancellationToken cancellationToken = default)
            where TRequest : class, IMessage
            where TResponse : class, IMessage
        {
            var correlationId = Guid.NewGuid();
            var replyToQueueName = $"reply-to-{correlationId}";
            var queueTimeout = TimeSpan.FromSeconds(Math.Max(maxTimeToWaitSeconds, 300)); // minimum allowed time for AutoDeleteOnIdle is 5 minutes
            var messageTimeout = TimeSpan.FromSeconds(Math.Max(maxTimeToWaitSeconds, 3)); // less than 3 seconds will likely result in the message expiring before being delivered

            try
            {
                await _administrationClient.CreateQueueAsync(
                    new CreateQueueOptions(replyToQueueName)
                    {
                        AutoDeleteOnIdle = queueTimeout
                    },
                    cancellationToken
                );

                var requestClient = _client.CreateSender<TRequest>();
                var receiverClient = _client.CreateReceiver(
                    replyToQueueName,
                    new ServiceBusReceiverOptions()
                    {
                        ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
                    }
                );

                await requestClient.SendMessageAsync(
                    new ServiceBusJsonMessage<TRequest>(message)
                    {
                        TimeToLive = messageTimeout,
                        ReplyTo = replyToQueueName
                    },
                    cancellationToken
                );

                var receiveMessageTask = receiverClient.ReceiveMessageAsync(queueTimeout, cancellationToken);
                var waitForTimeoutTask = Task.Delay(messageTimeout);
                await Task.WhenAny(new[] 
                {
                    receiveMessageTask, 
                    waitForTimeoutTask
                });

                if (!receiveMessageTask.IsCompleted)
                {
                    throw new TimeoutException($"Maximum timeout was reach ({messageTimeout.TotalSeconds}s) while waiting for message reply (correlationId: {correlationId})");
                }

                return receiveMessageTask.Result?.Body?.ToObjectFromJson<TResponse>();
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
