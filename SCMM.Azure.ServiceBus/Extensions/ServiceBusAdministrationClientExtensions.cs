using Azure.Messaging.ServiceBus.Administration;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Abstractions.Messaging.Attributes;
using System.Reflection;

namespace SCMM.Azure.ServiceBus.Extensions
{
    public static class ServiceBusAdministrationClientExtensions
    {
        public static Task<bool> TopicSubscriptionExistsAsync<T>(this ServiceBusAdministrationClient client) where T : IMessage
        {
            return TopicSubscriptionExistsAsync(client, typeof(T));
        }

        public static async Task<bool> TopicSubscriptionExistsAsync(this ServiceBusAdministrationClient client, Type messageType)
        {
            var topicName = messageType.GetCustomAttribute<TopicAttribute>()?.Name;
            if (!string.IsNullOrEmpty(topicName))
            {
                var subscriptionExists = await client.SubscriptionExistsAsync(topicName, Assembly.GetEntryAssembly().GetName().Name);
                return subscriptionExists.Value;
            }

            throw new ArgumentException(nameof(messageType), "Message type must have a [Topic] attribute declaration");
        }

        public static Task<SubscriptionProperties> CreateSubscriptionAsync<T>(this ServiceBusAdministrationClient client, Action<CreateSubscriptionOptions> optionsAction = null) where T : IMessage
        {
            return CreateTopicSubscriptionAsync(client, typeof(T), optionsAction);
        }

        public static async Task<SubscriptionProperties> CreateTopicSubscriptionAsync(this ServiceBusAdministrationClient client, Type messageType, Action<CreateSubscriptionOptions> optionsAction = null)
        {
            var topicName = messageType.GetCustomAttribute<TopicAttribute>()?.Name;
            if (!string.IsNullOrEmpty(topicName))
            {
                var options = new CreateSubscriptionOptions(topicName, Assembly.GetEntryAssembly().GetName().Name);
                optionsAction?.Invoke(options);
                var subscriptionProperties = await client.CreateSubscriptionAsync(options);
                return subscriptionProperties.Value;
            }

            throw new ArgumentException(nameof(messageType), "Message type must have a [Topic] attribute declaration");
        }
    }
}
