﻿using SCMM.Shared.Abstractions.Statistics;
using StackExchange.Redis;
using System.Text.Json;

namespace SCMM.Shared.Web.Statistics;

public class RedisStatisticsService : IStatisticsService
{
    private const string KeyFormat = "statistics:{0}";

    private readonly IDatabase _redis;

    public RedisStatisticsService(ConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    public async Task<T> GetAsync<T>(string key)
    {
        var value = await _redis.StringGetAsync(
            new RedisKey(String.Format(KeyFormat, key))
        );

        return (value.HasValue == true)
            ? JsonSerializer.Deserialize<T>(value.ToString())
            : default(T);
    }

    public async Task<IEnumerable<T>> GetListAsync<T>(string key)
    {
        var values = await _redis.ListRangeAsync(
            new RedisKey(String.Format(KeyFormat, key))
        );
        if (values?.Any() != true)
        {
            return null;
        }

        var results = new List<T>();
        foreach (var value in values)
        {
            results.Add((value.HasValue == true)
                ? JsonSerializer.Deserialize<T>(value.ToString())
                : default(T)
            );
        }

        return results;
    }

    public async Task<IDictionary<TKey, TValue>> GetDictionaryAsync<TKey, TValue>(string key)
    {
        var entries = await _redis.HashGetAllAsync(
            new RedisKey(String.Format(KeyFormat, key))
        );
        if (entries?.Any() != true)
        {
            return null;
        }

        var results = new Dictionary<TKey, TValue>();
        foreach (var entry in entries)
        {
            var entryKey = entry.Name;
            var entryValue = entry.Value;
            if (entryKey.HasValue != true)
            {
                continue;
            }

            results.Add(
                JsonSerializer.Deserialize<TKey>(entryKey.ToString()),
                JsonSerializer.Deserialize<TValue>(entryValue.ToString())
            );
        }

        return results;
    }

    public async Task SetAsync<T>(string key, T value)
    {
        await _redis.StringSetAsync(
            new RedisKey(String.Format(KeyFormat, key)),
            new RedisValue(JsonSerializer.Serialize(value))
        );;
    }

    public async Task SetListAsync<T>(string key, IEnumerable<T> value, bool deleteKeyBeforeSet = false)
    {
        if (deleteKeyBeforeSet)
        {
            await _redis.KeyDeleteAsync(
                new RedisKey(String.Format(KeyFormat, key))
            );
        }

        var values = value.Select(
            x => new RedisValue(JsonSerializer.Serialize(x))
        );

        await _redis.ListRightPushAsync(
            new RedisKey(String.Format(KeyFormat, key)),
            values.ToArray()
        );
    }

    public async Task SetDictionaryAsync<TKey, TValue>(string key, IDictionary<TKey,TValue> value, bool deleteKeyBeforeSet = false)
    {
        if (deleteKeyBeforeSet)
        {
            await _redis.KeyDeleteAsync(
                new RedisKey(String.Format(KeyFormat, key))
            );
        }

        var entries = value.Select(
            x => new HashEntry(
                new RedisValue(JsonSerializer.Serialize(x.Key)),
                new RedisValue(JsonSerializer.Serialize(x.Value))
            )
        );

        await _redis.HashSetAsync(
            new RedisKey(String.Format(KeyFormat, key)),
            entries.ToArray()
        );
    }
}