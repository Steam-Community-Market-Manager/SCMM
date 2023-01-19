using SCMM.Shared.Data.Store.Types;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Steam.Data.Store.Types
{
    [ComplexType]
    public class PersistableItemBundleDictionary : PersistableScalarDictionary<ulong, uint>
    {
        public PersistableItemBundleDictionary()
            : base()
        {
        }

        public PersistableItemBundleDictionary(IDictionary<ulong, uint> dictionary, IEqualityComparer<ulong> comparer = null)
            : base(dictionary, comparer)
        {
        }

        protected override ulong ConvertSingleKeyToRuntime(string rawKey)
        {
            return ulong.Parse(rawKey);
        }

        protected override uint ConvertSingleValueToRuntime(string rawValue)
        {
            return uint.Parse(rawValue);
        }

        protected override string ConvertSingleKeyToPersistable(ulong key)
        {
            return key.ToString();
        }

        protected override string ConvertSingleValueToPersistable(uint value)
        {
            return value.ToString();
        }
    }
}
