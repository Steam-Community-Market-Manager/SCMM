using AutoMapper;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.App;
using SCMM.Web.Data.Models.UI.Currency;
using SCMM.Web.Data.Models.UI.Language;
using System.Linq.Expressions;

namespace SCMM.Web.Server.Extensions
{
    public static class AutoMapperConfigurationExtensions
    {
        public const string ContextKeyUser = "user";
        public const string ContextKeyLanguage = "language";
        public const string ContextKeyCurrency = "currency";
        public const string ContextKeyApp = "app";

        public static void MapFromLanguage<TSource, TDestination, TLanguage>(this IMemberConfigurationExpression<TSource, TDestination, TLanguage> memberOptions) where TLanguage : LanguageDTO
        {
            memberOptions.MapFrom((src, dst, _, context) =>
            {
                return context.Items.ContainsKey(ContextKeyLanguage)
                    ? (TLanguage)context.Items[ContextKeyLanguage]
                    : null;
            });
        }

        public static void MapFromCurrency<TSource, TDestination, TCurrency>(this IMemberConfigurationExpression<TSource, TDestination, TCurrency> memberOptions) where TCurrency : CurrencyDTO
        {
            memberOptions.MapFrom((src, dst, _, context) =>
            {
                return context.Items.ContainsKey(ContextKeyCurrency)
                    ? (TCurrency)context.Items[ContextKeyCurrency]
                    : null;
            });
        }

        public static void MapFromApp<TSource, TDestination, TApp>(this IMemberConfigurationExpression<TSource, TDestination, TApp> memberOptions) where TApp : AppDTO
        {
            memberOptions.MapFrom((src, dst, _, context) =>
            {
                return context.Items.ContainsKey(ContextKeyApp)
                    ? (TApp)context.Items[ContextKeyApp]
                    : null;
            });
        }

        public static void MapFromUsingCurrencyExchange<TSource, TDestination>(this IMemberConfigurationExpression<TSource, TDestination, long> memberOptions, Expression<Func<TSource, long>> valueExpression, Expression<Func<TSource, IExchangeableCurrency>> currencyExpression)
        {
            memberOptions.MapFrom((src, dst, _, context) =>
            {
                try
                {
                    if (!context.Items.ContainsKey(ContextKeyCurrency))
                    {
                        return 0L;
                    }

                    var value = valueExpression.Compile().Invoke(src);
                    if (value == 0)
                    {
                        return 0L;
                    }

                    var valueCurrency = currencyExpression.Compile().Invoke(src);
                    if (valueCurrency == null)
                    {
                        return 0L;
                    }

                    var targetCurrency = (CurrencyDetailedDTO)context.Items[ContextKeyCurrency];
                    return targetCurrency.CalculateExchange(value, valueCurrency);
                }
                catch (Exception)
                {
                    return 0L;
                }
            });
        }

        public static void MapFromUsingCurrencyExchange<TSource, TDestination>(this IMemberConfigurationExpression<TSource, TDestination, long?> memberOptions, Expression<Func<TSource, long?>> valueExpression, Expression<Func<TSource, IExchangeableCurrency>> currencyExpression)
        {
            memberOptions.MapFrom((src, dst, _, context) =>
            {
                try
                {
                    if (!context.Items.ContainsKey(ContextKeyCurrency))
                    {
                        return (long?)null;
                    }

                    var value = valueExpression.Compile().Invoke(src);
                    if (value == null)
                    {
                        return (long?)null;
                    }

                    var valueCurrency = currencyExpression.Compile().Invoke(src);
                    if (valueCurrency == null)
                    {
                        return (long?)null;
                    }

                    var targetCurrency = (CurrencyDetailedDTO)context.Items[ContextKeyCurrency];
                    return targetCurrency.CalculateExchange(value.Value, valueCurrency);
                }
                catch (Exception)
                {
                    return (long?)null;
                }
            });
        }

        public static void MapFromUsingCurrencyTable<TSource, TDestination>(this IMemberConfigurationExpression<TSource, TDestination, long?> memberOptions, Expression<Func<TSource, IDictionary<string, long>>> valueExpression)
        {
            memberOptions.MapFrom((src, dst, _, context) =>
            {
                try
                {
                    var value = valueExpression.Compile().Invoke(src);
                    if (value == null)
                    {
                        return (long?)null;
                    }

                    var targetCurrency = (CurrencyDetailedDTO)context.Items[ContextKeyCurrency];
                    if (!value.ContainsKey(targetCurrency.Name))
                    {
                        return (long?)null;
                    }

                    return (long?)value[targetCurrency.Name];
                }
                catch (Exception)
                {
                    return (long?)null;
                }
            });
        }

        public static void MapFromUsingCurrencyTable<TSource, TDestination>(this IMemberConfigurationExpression<TSource, TDestination, long> memberOptions, Expression<Func<TSource, IDictionary<string, long>>> valueExpression)
        {
            memberOptions.MapFrom((src, dst, _, context) =>
            {
                try
                {
                    var value = valueExpression.Compile().Invoke(src);
                    if (value == null)
                    {
                        return 0L;
                    }

                    var targetCurrency = (CurrencyDetailedDTO)context.Items[ContextKeyCurrency];
                    if (!value.ContainsKey(targetCurrency.Name))
                    {
                        return 0L;
                    }

                    return (long)value[targetCurrency.Name];
                }
                catch (Exception)
                {
                    return 0L;
                }
            });
        }

        public static void MapFromUsingAssetBuyPrice<TSource, TDestination, TValue>(this IMemberConfigurationExpression<TSource, TDestination, TValue> memberOptions, Expression<Func<TSource, SteamAssetDescription>> assetDescriptionExpression, Expression<Func<MarketPrice, TValue>> propertyExpression)
        {
            memberOptions.MapFrom((src, dst, _, context) =>
            {
                try
                {
                    if (!context.Items.ContainsKey(ContextKeyCurrency))
                    {
                        return default;
                    }

                    var currency = (CurrencyDetailedDTO)context.Items[ContextKeyCurrency];
                    if (currency == null)
                    {
                        return default;
                    }

                    var assetDescription = assetDescriptionExpression.Compile().Invoke(src);
                    if (assetDescription == null)
                    {
                        return default;
                    }

                    var price = assetDescription.GetCheapestBuyPrice(currency);
                    if (price == null)
                    {
                        return default;
                    }

                    return propertyExpression.Compile().Invoke(price);
                }
                catch (Exception)
                {
                    return default;
                }
            });
        }

        public static void MapFromAssetBuyPrices<TSource, TDestination, T>(this IMemberConfigurationExpression<TSource, TDestination, T[]> memberOptions, Expression<Func<TSource, SteamAssetDescription>> assetDescriptionExpression, bool includeThirdPartyMarkets = true)
        {
            memberOptions.MapFrom((src, dst, _, context) =>
            {
                try
                {
                    if (!context.Items.ContainsKey(ContextKeyCurrency))
                    {
                        return null;
                    }

                    var currency = (CurrencyDetailedDTO)context.Items[ContextKeyCurrency];
                    if (currency == null)
                    {
                        return null;
                    }

                    var assetDescription = assetDescriptionExpression.Compile().Invoke(src);
                    if (assetDescription == null)
                    {
                        return null;
                    }

                    return assetDescription.GetBuyPrices(currency)
                        .Where(x => includeThirdPartyMarkets || x.IsFirstPartyMarket)
                        .OrderBy(x => x.Price)
                        .ToList();
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }
    }
}
