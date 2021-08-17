using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Shared.Data.Store.Types
{
    public abstract class PersistableScalarDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        public const string DefaultKeyValueSeperator = "=";
        public const string DefaultItemSeperator = "|";


        private readonly IDictionary<TKey, TValue> _data;

        protected PersistableScalarDictionary()
        {
            _data = new Dictionary<TKey, TValue>();
        }

        protected PersistableScalarDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer = null)
        {
            _data = new Dictionary<TKey, TValue>(dictionary ?? new Dictionary<TKey, TValue>(), comparer);
        }

        protected abstract TKey ConvertSingleKeyToRuntime(string rawKey);
        protected abstract TValue ConvertSingleValueToRuntime(string rawValue);
        protected abstract string ConvertSingleKeyToPersistable(TKey key);
        protected abstract string ConvertSingleValueToPersistable(TValue value);

        protected virtual string KeyValueSeperator => DefaultKeyValueSeperator;

        protected virtual string ItemSeperator => DefaultItemSeperator;

        [Required]
        public virtual string Serialised
        {
            get
            {
                var keyValues = _data.Select(x =>
                    $"{ConvertSingleKeyToPersistable(x.Key)}{KeyValueSeperator}{ConvertSingleValueToPersistable(x.Value)}"
                );
                return string.Join(ItemSeperator, keyValues);
            }
            set
            {
                _data.Clear();
                if (!string.IsNullOrEmpty(value))
                {
                    var pairs = value.Split(ItemSeperator, StringSplitOptions.None).Select(x => x.Split(KeyValueSeperator, StringSplitOptions.None)).ToList();
                    foreach (var pair in pairs)
                    {
                        _data.Add(
                            ConvertSingleKeyToRuntime(pair.FirstOrDefault()),
                            ConvertSingleValueToRuntime(pair.LastOrDefault())
                        );
                    }
                }
            }
        }

        #region IDictionary<T> Implementation

        public bool ContainsKey(TKey key)
        {
            return _data.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            _data.Add(key, value);
        }

        public bool Remove(TKey key)
        {
            return _data.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _data.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get => _data[key];
            set => _data[key] = value;
        }

        public ICollection<TKey> Keys => _data.Keys;

        public ICollection<TValue> Values => _data.Values;

        #endregion

        #region ICollection<T> Implementation

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _data.Add(item);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _data.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _data.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return _data.Remove(item);
        }

        public int Count => _data.Count;

        public bool IsReadOnly => _data.IsReadOnly;

        #endregion

        #region IEnumerable<T> Implementation

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        #endregion

        #region IEnumerable Implementation

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        #endregion
    }
}
