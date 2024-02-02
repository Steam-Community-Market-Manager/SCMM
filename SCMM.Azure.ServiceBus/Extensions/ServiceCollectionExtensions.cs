using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.Data.Models.Extensions;
using System.Reflection;

namespace SCMM.Azure.ServiceBus.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAzureServiceBus(this IServiceCollection serviceCollection, string connectionString)
        {
            serviceCollection.AddSingleton(new ServiceBusAdministrationClient(connectionString));
            serviceCollection.AddSingleton(new ServiceBusClient(connectionString));
            serviceCollection.AddSingleton<IServiceBus, AzureServiceBusClient>();
            return serviceCollection;
        }

        public static IServiceCollection AddMessages(this IServiceCollection services, params Assembly[] assemblies)
        {
            services.AddSingleton(x => new MessageHandlerTypeCollection(assemblies));
            foreach (var handlerType in assemblies.GetConcreteTypesAssignableTo(typeof(IMessageHandler<>)).ToArray())
            {
                foreach (var handlerInterface in handlerType.GetInterfacesOfGenericType(typeof(IMessageHandler<>)))
                {
                    services.AddTransient(handlerInterface, handlerType);
                }
            }

            return services;
        }
    }
}
