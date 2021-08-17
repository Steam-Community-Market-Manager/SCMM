using Azure.Messaging.ServiceBus;
using SCMM.Azure.ServiceBus.Attributes;
using System.Reflection;

namespace SCMM.Azure.ServiceBus.Extensions
{
    public static class ServiceBusClientExtensions
    {
        public static ServiceBusSender CreateSender<T>(this global::Azure.Messaging.ServiceBus.ServiceBusClient client) where T : IMessage
        {
            return CreateSender(client, typeof(T));
        }

        public static ServiceBusSender CreateSender(this global::Azure.Messaging.ServiceBus.ServiceBusClient client, Type messageType)
        {
            var topicName = messageType.GetCustomAttribute<TopicAttribute>()?.Name;
            var queueName = messageType.GetCustomAttribute<QueueAttribute>()?.Name;
            if (!string.IsNullOrEmpty(topicName) || !string.IsNullOrEmpty(queueName))
            {
                return client.CreateSender(topicName ?? queueName);
            }

            throw new ArgumentException(nameof(messageType), "Message type must have a [Queue] or [Topic] attribute declaration");
        }

        public static ServiceBusProcessor CreateProcessor<T>(this global::Azure.Messaging.ServiceBus.ServiceBusClient client, ServiceBusProcessorOptions options) where T : IMessage
        {
            return CreateProcessor(client, typeof(T), options);
        }

        public static ServiceBusProcessor CreateProcessor(this global::Azure.Messaging.ServiceBus.ServiceBusClient client, Type messageType, ServiceBusProcessorOptions options)
        {
            var topicName = messageType.GetCustomAttribute<TopicAttribute>()?.Name;
            if (!string.IsNullOrEmpty(topicName))
            {
                return client.CreateProcessor(topicName, Assembly.GetEntryAssembly().GetName().Name, options);
            }

            var queueName = messageType.GetCustomAttribute<QueueAttribute>()?.Name;
            if (!string.IsNullOrEmpty(queueName))
            {
                return client.CreateProcessor(queueName, options);
            }

            throw new ArgumentException(nameof(messageType), "Message type must have a [Queue] or [Topic] attribute declaration");
        }
    }
}
