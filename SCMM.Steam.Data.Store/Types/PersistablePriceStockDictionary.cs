using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.Data.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Steam.Data.Store.Types
{
    [ComplexType]
    public class PersistablePriceStockDictionary : PersistableScalarDictionary<PriceType, PriceStock>
    {
        public PersistablePriceStockDictionary()
            : base()
        {
        }

        public PersistablePriceStockDictionary(IDictionary<PriceType, PriceStock> dictionary, IEqualityComparer<PriceType> comparer = null)
            : base(dictionary, comparer)
        {
        }

        protected override PriceType ConvertSingleKeyToRuntime(string rawKey)
        {
            return Enum.Parse<PriceType>(rawKey);
        }

        protected override PriceStock ConvertSingleValueToRuntime(string rawValue)
        {
            var priceStock = rawValue.Split("@", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return new PriceStock()
            {
                Stock = (Int32.TryParse(priceStock.FirstOrDefault(), out _) ? Int32.Parse(priceStock.FirstOrDefault()) : null),
                Price = (Int32.TryParse(priceStock.LastOrDefault(), out _) ? Int32.Parse(priceStock.LastOrDefault()) : 0)
            };
        }

        protected override string ConvertSingleKeyToPersistable(PriceType key)
        {
            return key.ToString();
        }

        protected override string ConvertSingleValueToPersistable(PriceStock value)
        {
            return $"{value.Stock?.ToString() ?? "?"}@{value.Price}";
        }
    }

    public struct PriceStock
    {
        public int? Stock { get; set; }

        public long Price { get; set; }
    }
}
