using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Web.Server.Data.Types
{
    [ComplexType]
    public class PersistableStringDictionary : PersistableScalarDictionary<String, String>
    {
        public PersistableStringDictionary()
            : base()
        {
        }

        public PersistableStringDictionary(IDictionary<String, String> dictionary, IEqualityComparer<String> comparer = null)
            : base(dictionary, comparer)
        {
        }

        protected override string ConvertSingleKeyToRuntime(string rawKey)
        {
            return Uri.UnescapeDataString(rawKey);
        }

        protected override string ConvertSingleValueToRuntime(string rawValue)
        {
            return Uri.UnescapeDataString(rawValue);
        }

        protected override string ConvertSingleKeyToPersistable(string key)
        {
            return Uri.EscapeDataString(key);
        }

        protected override string ConvertSingleValueToPersistable(string value)
        {
            return Uri.EscapeDataString(value);
        }
    }
}
