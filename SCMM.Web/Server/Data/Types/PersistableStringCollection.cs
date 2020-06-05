using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Web.Server.Data.Types
{
    [ComplexType]
    public class PersistableStringCollection : PersistableScalarCollection<String>
    {
        public PersistableStringCollection()
            : base()
        {
        }

        public PersistableStringCollection(IEnumerable<String> collection)
            : base(collection)
        {
        }

        protected override string ConvertSingleValueToRuntime(string rawValue)
        {
            return rawValue;
        }

        protected override string ConvertSingleValueToPersistable(string value)
        {
            return value;
        }
    }
}
