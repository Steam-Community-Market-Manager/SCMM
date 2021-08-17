using SCMM.Shared.Data.Store.Types;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Steam.Data.Store.Types
{
    [ComplexType]
    public class PersistableAssetQuantityDictionary : PersistableScalarDictionary<string, uint>
    {
        public PersistableAssetQuantityDictionary()
            : base()
        {
        }

        public PersistableAssetQuantityDictionary(IDictionary<string, uint> dictionary, IEqualityComparer<string> comparer = null)
            : base(dictionary, comparer)
        {
        }

        protected override string ConvertSingleKeyToRuntime(string rawKey)
        {
            return Uri.UnescapeDataString(rawKey);
        }

        protected override uint ConvertSingleValueToRuntime(string rawValue)
        {
            return uint.Parse(rawValue);
        }

        protected override string ConvertSingleKeyToPersistable(string key)
        {
            return Uri.EscapeDataString(key);
        }

        protected override string ConvertSingleValueToPersistable(uint value)
        {
            return value.ToString();
        }
    }
}
