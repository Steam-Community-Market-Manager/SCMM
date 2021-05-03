using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Shared.Data.Store.Types
{
    [ComplexType]
    public class PersistableStringCollection : PersistableScalarCollection<string>
    {
        public PersistableStringCollection()
            : base()
        {
        }

        public PersistableStringCollection(IEnumerable<string> collection)
            : base(collection)
        {
        }

        protected override string ConvertSingleValueToRuntime(string rawValue)
        {
            return Uri.UnescapeDataString(rawValue);
        }

        protected override string ConvertSingleValueToPersistable(string value)
        {
            return Uri.EscapeDataString(value);
        }
    }
}
