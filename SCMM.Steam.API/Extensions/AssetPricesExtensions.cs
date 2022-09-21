using Steam.Models.SteamEconomy;
using System.Reflection;

namespace SCMM.Steam.API.Extensions;

public static  class AssetPricesExtensions
{
    public static IDictionary<string, long> ToDictionary(this AssetPricesModel prices)
    {
        return (prices == null) 
            ? new Dictionary<string, long>() 
            : prices.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(
                    k => k.Name,
                    prop => (long)((uint)prop.GetValue(prices, null))
                );
    }
}
