using Microsoft.Extensions.DependencyInjection;
using SCMM.Redis;

namespace SCMM.Redis.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRedis(this IServiceCollection serviceCollection, string connectionString)
        {
            serviceCollection.AddSingleton(async x => RedisConnection.InitializeAsync(connectionString));
            return serviceCollection;
        }
    }
}
