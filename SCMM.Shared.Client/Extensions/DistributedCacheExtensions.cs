using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace SCMM.Shared.Client.Extensions;

public static class DistributedCacheExtensions
{
    public static async Task<T> GetJsonAsync<T>(this IDistributedCache cache, string key, CancellationToken token = default)
    {
        var value = await cache.GetAsync(key, token);
        if (value?.Length > 0)
        {
            return JsonSerializer.Deserialize<T>(Encoding.Unicode.GetString(value));
        }

        return default;
    }

    public static async Task SetJsonAsync<T>(this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions options = null, CancellationToken token = default)
    {
        if (!Equals(default(T), value))
        {
            var jsonValue = Encoding.Unicode.GetBytes(JsonSerializer.Serialize(value));
            await cache.SetAsync(key, jsonValue, options ?? new DistributedCacheEntryOptions(), token);
        }
        else
        {
            await cache.RemoveAsync(key, token);
        }
    }
}
