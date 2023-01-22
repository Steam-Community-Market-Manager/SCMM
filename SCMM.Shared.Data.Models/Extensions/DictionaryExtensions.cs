namespace SCMM.Shared.Data.Models.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }
            else
            {
                return defaultValue;
            }
        }

        public static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Predicate<KeyValuePair<TKey, TValue>> match)
        {
            var toBeRemoved = dictionary.Where(x => match.Invoke(x)).ToArray();
            foreach (var item in toBeRemoved)
            {
                dictionary.Remove(item);
            }
        }
    }
}
