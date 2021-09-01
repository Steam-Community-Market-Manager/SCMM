using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.Data.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Steam.Data.Store.Types
{
    [ComplexType]
    public class PersistableMediaDictionary : PersistableScalarDictionary<string, SteamMediaType>
    {
        public PersistableMediaDictionary()
            : base()
        {
        }

        public PersistableMediaDictionary(IDictionary<string, SteamMediaType> dictionary, IEqualityComparer<string> comparer = null)
            : base(dictionary, comparer)
        {
        }

        protected override string ConvertSingleKeyToRuntime(string rawKey)
        {
            return Uri.UnescapeDataString(rawKey);
        }

        protected override SteamMediaType ConvertSingleValueToRuntime(string rawValue)
        {
            return (SteamMediaType) Enum.Parse(typeof(SteamMediaType), rawValue, true);
        }

        protected override string ConvertSingleKeyToPersistable(string key)
        {
            return Uri.EscapeDataString(key);
        }

        protected override string ConvertSingleValueToPersistable(SteamMediaType value)
        {
            return value.ToString();
        }
    }
}
