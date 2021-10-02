using SCMM.Shared.Data.Store.Types;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Steam.Data.Store.Types
{
    [ComplexType]
    public class PersistablePriceCollection : PersistableScalarCollection<long>
    {
        public PersistablePriceCollection()
            : base()
        {
        }

        public PersistablePriceCollection(IEnumerable<long> collection)
            : base(collection)
        {
        }

        protected override long ConvertSingleValueToRuntime(string rawValue)
        {
            return long.Parse(rawValue);
        }

        protected override string ConvertSingleValueToPersistable(long value)
        {
            return value.ToString();
        }
    }
}
