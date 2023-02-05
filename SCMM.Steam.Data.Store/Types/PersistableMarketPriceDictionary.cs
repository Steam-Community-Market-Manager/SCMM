using SCMM.Shared.Data.Store.Types;
using SCMM.Steam.Data.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCMM.Steam.Data.Store.Types
{
    [ComplexType]
    public class PersistableMarketPriceDictionary : PersistableScalarDictionary<MarketType, PriceWithSupply>
    {
        public PersistableMarketPriceDictionary()
            : base()
        {
        }

        public PersistableMarketPriceDictionary(IDictionary<MarketType, PriceWithSupply> dictionary, IEqualityComparer<MarketType> comparer = null)
            : base(dictionary, comparer)
        {
        }

        protected override MarketType ConvertSingleKeyToRuntime(string rawKey)
        {
            MarketType key = MarketType.Unknown;
            Enum.TryParse<MarketType>(rawKey, out key);
            return key;
        }

        protected override PriceWithSupply ConvertSingleValueToRuntime(string rawValue)
        {
            var priceWithSupply = rawValue.Split("x", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return new PriceWithSupply()
            {
                Price = (Int32.TryParse(priceWithSupply.FirstOrDefault(), out _) ? Int32.Parse(priceWithSupply.FirstOrDefault()) : 0),
                Supply = (priceWithSupply.Length > 1 ? Int32.Parse(priceWithSupply.LastOrDefault()) : null)
            };
        }

        protected override string ConvertSingleKeyToPersistable(MarketType key)
        {
            return key.ToString();
        }

        protected override string ConvertSingleValueToPersistable(PriceWithSupply value)
        {
            return (value.Supply > 0)
                ? $"{value.Price}x{value.Supply.Value}"
                : $"{value.Price}";
        }
    }

    public struct PriceWithSupply
    {
        public long Price { get; set; }
        public int? Supply { get; set; }
    }
}
