namespace SCMM.Shared.Abstractions.Statistics;

public interface IStatisticsService
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
    Task PatchAsync<T>(string key, Action<T> updateValue) where T : new();

    Task<IEnumerable<T>> GetListAsync<T>(string key);
    Task SetListAsync<T>(string key, IEnumerable<T> value, bool deleteKeyBeforeSet = false);

    Task<IDictionary<TKey, TValue>> GetDictionaryAsync<TKey, TValue>(string key);
    Task<TValue> GetDictionaryValueAsync<TKey, TValue>(string key, TKey field);
    Task SetDictionaryAsync<TKey, TValue>(string key, IDictionary<TKey, TValue> value, bool deleteKeyBeforeSet = false);
    Task SetDictionaryValueAsync<TKey, TValue>(string key, TKey field, TValue value);
    Task PatchDictionaryValueAsync<TKey, TValue>(string key, TKey field, Action<TValue> updateValue) where TValue : new();
}
