using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Azure.Messaging.ServiceBus;
using SCMM.Shared.Data.Models.Extensions;
using System;
using System.Text.Json;
using SCMM.Shared.Azure.ServiceBus.Attributes;
using System.Reflection;
using SCMM.Shared.Azure.ServiceBus.Extensions;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus.Administration;

namespace SCMM.Shared.Azure.ServiceBus.Middleware
{
    public class ServiceBusProcessorMiddleware : IAsyncDisposable
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ServiceBusProcessorMiddleware> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ServiceBusAdministrationClient _serviceBusAdministrationClient;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly List<ServiceBusProcessor> _serviceBusProcessors;
        private bool disposedValue;

        public ServiceBusProcessorMiddleware(RequestDelegate next, ILogger<ServiceBusProcessorMiddleware> logger, IServiceScopeFactory scopeFactory, ServiceBusAdministrationClient serviceBusAdministrationClient, ServiceBusClient serviceBusClient, MessageHandlerTypeCollection messageHandlerTypeCollection)
        {
            _next = next;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _serviceBusAdministrationClient = serviceBusAdministrationClient;
            _serviceBusClient = serviceBusClient;
            _serviceBusProcessors = new List<ServiceBusProcessor>();
            _ = CreateMissingTopicSubscriptions(messageHandlerTypeCollection).ContinueWith(x =>
            {
                if (x.IsFaulted && x.Exception != null)
                {
                    _logger.LogError(x.Exception, "Failed to create topic subscriptions, some messages may not get handled");
                }
            });
            _ = StartMessageProcessors(messageHandlerTypeCollection).ContinueWith(x =>
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
                    await StopMessageProcessors();
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

        private async Task CreateMissingTopicSubscriptions(MessageHandlerTypeCollection messageHandlerTypeCollection)
        {
            // Instantiate processors
            var handlerAssemblies = messageHandlerTypeCollection.Assemblies.ToArray();
            foreach (var handlerType in handlerAssemblies.GetTypesAssignableTo(typeof(IMessageHandler<>)))
            {
                foreach (var handlerInterface in handlerType.GetInterfacesOfGenericType(typeof(IMessageHandler<>)))
                {
                    var messageType = handlerInterface.GenericTypeArguments.FirstOrDefault();
                    if (messageType == null || !messageType.IsAssignableTo(typeof(IMessage)) || messageType.GetCustomAttribute<TopicAttribute>() == null)
                    {
                        continue; // not a supported message subscription
                    }

                    var topicSubscriptionExists = await _serviceBusAdministrationClient.SubscriptionExistsAsync(messageType);
                    if (!topicSubscriptionExists)
                    {
                        await _serviceBusAdministrationClient.CreateSubscriptionAsync(messageType, options =>
                        {
                            options.AutoDeleteOnIdle = TimeSpan.FromDays(7); // auto-delete if no messages for 7 days
                        });
                    }
                }
            }
        }

        private async Task StartMessageProcessors(MessageHandlerTypeCollection messageHandlerTypeCollection)
        {
            // Instantiate processors
            var processors = new List<ServiceBusProcessor>();
            var handlerAssemblies = messageHandlerTypeCollection.Assemblies.ToArray();
            foreach (var handlerType in handlerAssemblies.GetTypesAssignableTo(typeof(IMessageHandler<>)))
            {
                foreach (var handlerInterface in handlerType.GetInterfacesOfGenericType(typeof(IMessageHandler<>)))
                {
                    var messageType = handlerInterface.GenericTypeArguments.FirstOrDefault();
                    if (messageType == null || !messageType.IsAssignableTo(typeof(IMessage)))
                    {
                        continue; // not a supported message handler
                    }

                    var processor = _serviceBusClient.CreateProcessor(messageType, new ServiceBusProcessorOptions()
                    {
                        PrefetchCount = handlerType.GetCustomAttribute<PrefetchAttribute>()?.PrefetchCount ?? 0,
                        MaxConcurrentCalls = handlerType.GetCustomAttribute<ConcurrencyAttribute>()?.MaxConcurrentCalls ?? 1
                    });

                    processor.ProcessMessageAsync += async (x) =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var message = JsonSerializer.Deserialize(x.Message.Body.ToArray(), messageType);
                        var handler = scope.ServiceProvider.GetRequiredService(handlerInterface);
                        var handleMethod = handler.GetType().GetMethod("HandleAsync");
                        var task = (Task)handleMethod.Invoke(handler, new object[] { message });
                        await task;
                    };

                    processor.ProcessErrorAsync += (x) =>
                    {
                        _logger.LogError(x.Exception, $"Failed to process service bus message (source: {x.ErrorSource}, queue: {x.EntityPath}, type: {messageType}, handler: {handlerInterface})");
                        return Task.CompletedTask;
                    };

                    processors.Add(processor);
                }
            }

            await Task.WhenAll(processors.Select(x => x.StartProcessingAsync()));
            lock (_serviceBusProcessors)
            {
                _serviceBusProcessors.AddRange(processors);
            }
        }

        private async Task StopMessageProcessors()
        {
            IList<ServiceBusProcessor> processors;
            lock (_serviceBusProcessors)
            {
                processors = _serviceBusProcessors.ToList();
                _serviceBusProcessors.Clear();
            }

            await Task.WhenAll(processors.Select(x => x.StopProcessingAsync()));
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
