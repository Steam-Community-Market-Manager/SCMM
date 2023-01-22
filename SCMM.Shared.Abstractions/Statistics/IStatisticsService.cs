namespace SCMM.Shared.Abstractions.Statistics;

public interface IStatisticsService
{
    Task<T> GetAsync<T>(string key);
    Task<IEnumerable<T>> GetListAsync<T>(string key);
    Task<IDictionary<TKey, TValue>> GetDictionaryAsync<TKey, TValue>(string key);
    Task<TValue> GetDictionaryValueAsync<TKey, TValue>(string key, TKey field);

    Task SetAsync<T>(string key, T value);
    Task SetListAsync<T>(string key, IEnumerable<T> value, bool deleteKeyBeforeSet = false);
    Task SetDictionaryAsync<TKey, TValue>(string key, IDictionary<TKey, TValue> value, bool deleteKeyBeforeSet = false);
    Task SetDictionaryValueAsync<TKey, TValue>(string key, TKey field, TValue value);
    Task UpdateDictionaryValueAsync<TKey, TValue>(string key, TKey field, Action<TValue> updateValue);
}
