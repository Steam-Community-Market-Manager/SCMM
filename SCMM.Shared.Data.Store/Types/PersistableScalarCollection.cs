﻿using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace SCMM.Shared.Data.Store.Types
{
    public abstract class PersistableScalarCollection<T> : ICollection<T>
    {
        public const string DefaultValueSeperator = "|";

        private readonly ICollection<T> _data;

        protected PersistableScalarCollection()
        {
            _data = new List<T>();
        }

        protected PersistableScalarCollection(IEnumerable<T> collection)
        {
            _data = new List<T>(collection ?? new T[0]);
        }

        protected abstract T ConvertSingleValueToRuntime(string rawValue);

        protected abstract string ConvertSingleValueToPersistable(T value);

        protected virtual string ValueSeperator => DefaultValueSeperator;

        [Required]
        public virtual string Serialised
        {
            get
            {
                return string.Join(ValueSeperator, _data.Select(x => ConvertSingleValueToPersistable(x)));
            }
            set
            {
                _data.Clear();
                if (!string.IsNullOrEmpty(value))
                {
                    var values = value.Split(ValueSeperator, StringSplitOptions.None).Select(x => ConvertSingleValueToRuntime(x)).ToArray();
                    foreach (var x in values)
                    {
                        _data.Add(x);
                    }
                }
            }
        }

        #region ICollection<T> Implementation

        public void Add(T item)
        {
            _data.Add(item);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(T item)
        {
            return _data.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _data.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _data.Remove(item);
        }

        public int Count => _data.Count;

        public bool IsReadOnly => false;

        #endregion

        #region IEnumerable<T> Implementation

        public IEnumerator<T> GetEnumerator()
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
