using Microsoft.Extensions.DependencyInjection;
using SCMM.Redis.Client;

namespace SCMM.Redis.Client.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRedis(this IServiceCollection serviceCollection, string connectionString)
        {
            serviceCollection.AddTransient<RedisConnection>((x) =>
            {
                var connectionTask = RedisConnection.InitializeAsync(connectionString);
                Task.WaitAll(connectionTask);
                return connectionTask.Result;
            });

            return serviceCollection;
        }
    }
}
