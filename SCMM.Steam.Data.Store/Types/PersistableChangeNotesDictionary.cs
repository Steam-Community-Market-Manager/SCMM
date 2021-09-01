using SCMM.Shared.Data.Store.Types;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Steam.Data.Store.Types
{
    [ComplexType]
    public class PersistableChangeNotesDictionary : PersistableScalarDictionary<DateTimeOffset, string>
    {
        public PersistableChangeNotesDictionary()
            : base()
        {
        }

        public PersistableChangeNotesDictionary(IDictionary<DateTimeOffset, string> dictionary, IEqualityComparer<DateTimeOffset> comparer = null)
            : base(dictionary, comparer)
        {
        }

        protected override DateTimeOffset ConvertSingleKeyToRuntime(string rawKey)
        {
            return new DateTimeOffset(long.Parse(rawKey), TimeZoneInfo.Utc.BaseUtcOffset);
        }

        protected override string ConvertSingleValueToRuntime(string rawValue)
        {
            return rawValue;
        }

        protected override string ConvertSingleKeyToPersistable(DateTimeOffset key)
        {
            return key.UtcDateTime.Ticks.ToString();
        }

        protected override string ConvertSingleValueToPersistable(string value)
        {
            return value.ToString();
        }
    }
}
