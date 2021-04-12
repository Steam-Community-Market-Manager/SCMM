using SCMM.Data.Shared.Store.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Steam.Data.Store.Types
{
    [ComplexType]
    public class PersistableHourlyGraphDataSet : PersistableScalarDictionary<DateTime, double>
    {
        public PersistableHourlyGraphDataSet()
            : base()
        {
        }

        public PersistableHourlyGraphDataSet(IDictionary<DateTime, double> dictionary, IEqualityComparer<DateTime> comparer = null)
            : base(dictionary, comparer)
        {
        }

        protected override DateTime ConvertSingleKeyToRuntime(string rawKey)
        {
            try
            {
                return DateTime.ParseExact(rawKey, "dd-MM-yyyy:HH", null);
            }
            catch (Exception)
            {
                return DateTime.ParseExact(rawKey, "dd-MM-yyyy", null);
            }
        }

        protected override double ConvertSingleValueToRuntime(string rawValue)
        {
            return double.Parse(rawValue);
        }

        protected override string ConvertSingleKeyToPersistable(DateTime key)
        {
            return key.ToString("dd-MM-yyyy:HH");
        }

        protected override string ConvertSingleValueToPersistable(double value)
        {
            return value.ToString();
        }
    }
}
