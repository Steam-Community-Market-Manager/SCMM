using SCMM.Shared.Data.Store.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Steam.Data.Store.Types
{
    [ComplexType]
    public class PersistablePriceDictionary : PersistableScalarDictionary<string, long>
    {
        public PersistablePriceDictionary()
            : base()
        {
        }

        public PersistablePriceDictionary(IDictionary<string, long> dictionary, IEqualityComparer<string> comparer = null)
            : base(dictionary, comparer)
        {
        }

        protected override string ConvertSingleKeyToRuntime(string rawKey)
        {
            return Uri.UnescapeDataString(rawKey);
        }

        protected override long ConvertSingleValueToRuntime(string rawValue)
        {
            return long.Parse(rawValue);
        }

        protected override string ConvertSingleKeyToPersistable(string key)
        {
            return Uri.EscapeDataString(key);
        }

        protected override string ConvertSingleValueToPersistable(long value)
        {
            return value.ToString();
        }
    }
}
