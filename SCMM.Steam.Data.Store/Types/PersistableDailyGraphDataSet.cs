using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using SCMM.Shared.Data.Store.Types;

namespace SCMM.Steam.Data.Store.Types
{
    [ComplexType]
    public class PersistableDailyGraphDataSet : PersistableScalarDictionary<DateTime, double>
    {
        public PersistableDailyGraphDataSet()
            : base()
        {
        }

        public PersistableDailyGraphDataSet(IDictionary<DateTime, double> dictionary, IEqualityComparer<DateTime> comparer = null)
            : base(dictionary, comparer)
        {
        }

        protected override DateTime ConvertSingleKeyToRuntime(string rawKey)
        {
            return DateTime.ParseExact(rawKey, "dd-MM-yyyy", null);
        }

        protected override double ConvertSingleValueToRuntime(string rawValue)
        {
            return double.Parse(rawValue);
        }

        protected override string ConvertSingleKeyToPersistable(DateTime key)
        {
            return key.ToString("dd-MM-yyyy");
        }

        protected override string ConvertSingleValueToPersistable(double value)
        {
            return value.ToString();
        }
    }
}
