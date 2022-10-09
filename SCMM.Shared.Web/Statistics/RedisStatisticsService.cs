using SCMM.Shared.Abstractions.Statistics;
using StackExchange.Redis;

namespace SCMM.Shared.Web.Statistics;

public class RedisStatisticsService : IStatisticsService
{
    private readonly ConnectionMultiplexer _redis;

    public RedisStatisticsService(ConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public Task<T> GetAsync<T>(string key)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ModifyAsync<T>(string key, Func<T, T> updateFunc)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SetAsync<T>(string key, T stat)
    {
        throw new NotImplementedException();
    }
}
