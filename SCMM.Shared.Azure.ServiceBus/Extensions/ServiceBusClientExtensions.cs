using Azure.Messaging.ServiceBus;
using SCMM.Shared.Azure.ServiceBus.Attributes;
using System;
using System.Reflection;

namespace SCMM.Shared.Azure.ServiceBus.Extensions
{
    public static class ServiceBusClientExtensions
    {
        public static ServiceBusSender CreateSender<T>(this ServiceBusClient client) where T : IMessage
        {
            return client.CreateSender(GetQueueNameForMessageType(typeof(T)));
        }

        public static ServiceBusSender CreateSender(this ServiceBusClient client, Type messageType)
        {
            return client.CreateSender(GetQueueNameForMessageType(messageType));
        }

        public static ServiceBusProcessor CreateProcessor<T>(this ServiceBusClient client, ServiceBusProcessorOptions options) where T : IMessage
        {
            return client.CreateProcessor(GetQueueNameForMessageType(typeof(T)), options);
        }

        public static ServiceBusProcessor CreateProcessor(this ServiceBusClient client, Type messageType, ServiceBusProcessorOptions options)
        {
            return client.CreateProcessor(GetQueueNameForMessageType(messageType), options);
        }

        private static string GetQueueNameForMessageType(Type messageType)
        {
            var queueAttribute = messageType.GetCustomAttribute<QueueAttribute>()?.Name;
            var topicAttribute = messageType.GetCustomAttribute<TopicAttribute>()?.Name;
            if (String.IsNullOrEmpty(queueAttribute) && String.IsNullOrEmpty(topicAttribute))
            {
                throw new ArgumentException(nameof(messageType), "Message type must have a [Queue] or [Topic] attribute declaration");
            }

            return queueAttribute ?? topicAttribute;
        }
    }
}
