using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCMM.Azure.ServiceBus.Attributes;
using SCMM.Azure.ServiceBus.Extensions;
using SCMM.Shared.Data.Models.Extensions;
using System.Reflection;
using System.Text.Json;

namespace SCMM.Azure.ServiceBus.Middleware
{
    public class ServiceBusProcessorMiddleware : IAsyncDisposable
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ServiceBusProcessorMiddleware> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly List<ServiceBusProcessor> _serviceBusProcessors;
        private readonly global::Azure.Messaging.ServiceBus.Administration.ServiceBusAdministrationClient _serviceBusAdministrationClient;
        private readonly global::Azure.Messaging.ServiceBus.ServiceBusClient _serviceBusClient;
        private bool disposedValue;

        public ServiceBusProcessorMiddleware(RequestDelegate next, ILogger<ServiceBusProcessorMiddleware> logger, IServiceScopeFactory scopeFactory,
            global::Azure.Messaging.ServiceBus.Administration.ServiceBusAdministrationClient serviceBusAdministrationClient, global::Azure.Messaging.ServiceBus.ServiceBusClient serviceBusClient,
            MessageHandlerTypeCollection messageHandlerTypeCollection)
        {
            _next = next;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _serviceBusProcessors = new List<ServiceBusProcessor>();
            _serviceBusAdministrationClient = serviceBusAdministrationClient;
            _serviceBusClient = serviceBusClient;

            // Build a dictionary of handler interfaces and their associated concrete implementation type
            var handlerMappings = new Dictionary<Type, Type>();
            var handlerAssemblies = messageHandlerTypeCollection.Assemblies.ToArray();
            var handlerTypes = handlerAssemblies.GetTypesAssignableTo(typeof(IMessageHandler<>));
            foreach (var handlerType in handlerTypes)
            {
                var handlerInterfaces = handlerType.GetInterfacesOfGenericType(typeof(IMessageHandler<>));
                foreach (var handlerInterface in handlerInterfaces)
                {
                    handlerMappings[handlerInterface] = handlerType;
                }
            }

            _ = CreateMissingQueuesAsync(handlerMappings).ContinueWith(x =>
            {
                if (x.IsFaulted && x.Exception != null)
                {
                    _logger.LogError(x.Exception, "Failed to create queues, some messages may not get handled");
                }
            });
            _ = CreateMissingTopicsAndSubscriptionsAsync(handlerMappings).ContinueWith(x =>
            {
                if (x.IsFaulted && x.Exception != null)
                {
                    _logger.LogError(x.Exception, "Failed to create topic subscriptions, some messages may not get handled");
                }
            });
            _ = StartMessageProcessorsAsync(handlerMappings).ContinueWith(x =>
            {
                if (x.IsFaulted && x.Exception != null)
                {
                    _logger.LogError(x.Exception, "Failed to start service bus message processors, some messages may not get handled");
                }
            });
        }

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    await StopMessageProcessorsAsync();
                }
                disposedValue = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }

        public Task Invoke(HttpContext httpContext)
        {
            return _next(httpContext);
        }

        private async Task CreateMissingQueuesAsync(IDictionary<Type, Type> handlerMappings)
        {
            foreach (var handlerMapping in handlerMappings)
            {
                var handlerInterface = handlerMapping.Key;
                var messageType = handlerInterface.GenericTypeArguments.FirstOrDefault();
                if (messageType == null || !messageType.IsAssignableTo(typeof(IMessage)) || messageType.GetCustomAttribute<QueueAttribute>() == null)
                {
                    continue; // this is not a queue message
                }

                var queueName = messageType.GetCustomAttribute<QueueAttribute>()?.Name;
                var queueExists = await _serviceBusAdministrationClient.QueueExistsAsync(queueName.ToLower());
                if (!queueExists)
                {
                    await _serviceBusAdministrationClient.CreateQueueAsync(new CreateQueueOptions(queueName)
                    {
                        DeadLetteringOnMessageExpiration = true,
                        DefaultMessageTimeToLive = TimeSpan.FromDays(1),
                        DuplicateDetectionHistoryTimeWindow = TimeSpan.FromDays(1),
                        LockDuration = TimeSpan.FromMinutes(1),
                        MaxDeliveryCount = 3,
                        MaxSizeInMegabytes = 1024,
                        RequiresDuplicateDetection = true
                    });
                }
            }
        }

        private async Task CreateMissingTopicsAndSubscriptionsAsync(IDictionary<Type, Type> handlerMappings)
        {
            foreach (var handlerMapping in handlerMappings)
            {
                var handlerInterface = handlerMapping.Key;
                var messageType = handlerInterface.GenericTypeArguments.FirstOrDefault();
                if (messageType == null || !messageType.IsAssignableTo(typeof(IMessage)) || messageType.GetCustomAttribute<TopicAttribute>() == null)
                {
                    continue; // this is not a topic message
                }

                var topicSubscriptionExists = await _serviceBusAdministrationClient.TopicSubscriptionExistsAsync(messageType);
                if (!topicSubscriptionExists)
                {
                    await _serviceBusAdministrationClient.CreateTopicSubscriptionAsync(messageType, options =>
                    {
                        options.AutoDeleteOnIdle = TimeSpan.FromDays(7); // auto-delete if no messages for 7 days
                    });
                }
            }
        }

        private async Task StartMessageProcessorsAsync(IDictionary<Type, Type> handlerMappings)
        {
            var processors = new List<ServiceBusProcessor>();
            foreach (var handlerMapping in handlerMappings)
            {
                var handlerInterface = handlerMapping.Key;
                var handlerType = handlerMapping.Value;
                var messageType = handlerInterface.GenericTypeArguments.FirstOrDefault();
                if (messageType == null || !messageType.IsAssignableTo(typeof(IMessage)))
                {
                    continue; // this is not a valid message handler
                }

                var processor = _serviceBusClient.CreateProcessor(messageType, new ServiceBusProcessorOptions()
                {
                    PrefetchCount = handlerType.GetCustomAttribute<PrefetchAttribute>()?.PrefetchCount ?? 0,
                    MaxConcurrentCalls = handlerType.GetCustomAttribute<ConcurrencyAttribute>()?.MaxConcurrentCalls ?? 1
                });

                processor.ProcessMessageAsync += ProcessMessageAsync;
                processor.ProcessErrorAsync += ProcessErrorAsync;
                processors.Add(processor);
            }

            await Task.WhenAll(processors.Select(x => x.StartProcessingAsync()));
            lock (_serviceBusProcessors)
            {
                _serviceBusProcessors.AddRange(processors);
            }
        }

        private async Task StopMessageProcessorsAsync()
        {
            IList<ServiceBusProcessor> processors;
            lock (_serviceBusProcessors)
            {
                processors = _serviceBusProcessors.ToList();
                _serviceBusProcessors.Clear();
            }

            await Task.WhenAll(processors.Select(x => x.StopProcessingAsync()));
        }

        private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            var context = new MessageContext(_serviceBusClient)
            {
                MessageId = args.Message.MessageId,
                MessageType = Type.GetType((string) args.Message.ApplicationProperties.GetValueOrDefault(IMessage.ApplicationPropertyType, typeof(IMessage).AssemblyQualifiedName)),
                ReplyTo = args.Message.ReplyTo
            };

            using (var scope = _scopeFactory.CreateScope())
            {
                var message = JsonSerializer.Deserialize(args.Message.Body.ToArray(), context.MessageType);
                var handlerType = typeof(IMessageHandler<>).MakeGenericType(context.MessageType);
                if (handlerType == null)
                {
                    throw new Exception($"Unable to process service bus message (id: {args.Message.MessageId}), handler type not found ");
                }

                var handlerInstance = scope.ServiceProvider.GetService(handlerType);
                if (handlerInstance == null)
                {
                    throw new Exception($"Unable to process service bus message (id: {args.Message.MessageId}), handler cannot be instantiated");
                }

                var handlerMethod = handlerInstance.GetType().GetMethod("HandleAsync");
                if (handlerMethod == null)
                {
                    throw new Exception($"Unable to process service bus message (id: {args.Message.MessageId}), handler method not found");
                }

                var task = (Task)handlerMethod.Invoke(
                    handlerInstance,
                    new object[]
                    {
                    message,
                    context
                    }
                );

                await task;
            }
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, $"Error while processing service bus message (id: {args.Identifier}, namespace: {args.FullyQualifiedNamespace}, entityPath: {args.EntityPath}, source: {args.ErrorSource})");
            return Task.CompletedTask;
        }
    }

    public static class ServiceBusProcessorMiddlewareExtensions
    {
        public static IApplicationBuilder UseAzureServiceBusProcessor(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ServiceBusProcessorMiddleware>();
        }
    }
}
