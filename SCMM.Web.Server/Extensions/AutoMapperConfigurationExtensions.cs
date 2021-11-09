using AutoMapper;
using SCMM.Shared.Data.Models;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Currency;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Data.Models.UI.Language;
using System.Linq.Expressions;

namespace SCMM.Web.Server.Extensions
{
    public static class AutoMapperConfigurationExtensions
    {
        public const string ContextKeyUser = "user";
        public const string ContextKeyLanguage = "language";
        public const string ContextKeyCurrency = "currency";

        public static void MapFromLanguage<TSource, TDestination, TLanguage>(this IMemberConfigurationExpression<TSource, TDestination, TLanguage> memberOptions) where TLanguage : LanguageDTO
        {
            memberOptions.MapFrom((src, dst, _, context) =>
            {
                return context.Items.ContainsKey(ContextKeyLanguage)
                    ? (TLanguage)context.Options.Items[ContextKeyLanguage]
                    : null;
            });
        }

        public static void MapFromCurrency<TSource, TDestination, TCurrency>(this IMemberConfigurationExpression<TSource, TDestination, TCurrency> memberOptions) where TCurrency : CurrencyDTO
        {
            memberOptions.MapFrom((src, dst, _, context) =>
            {
                return context.Items.ContainsKey(ContextKeyCurrency)
                    ? (TCurrency)context.Options.Items[ContextKeyCurrency]
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

        public static void MapFromUsingAssetPrice<TSource, TDestination, TValue>(this IMemberConfigurationExpression<TSource, TDestination, TValue> memberOptions, Expression<Func<TSource, SteamAssetDescription>> assetDescriptionExpression, Expression<Func<Price, TValue>> propertyExpression)
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

                    var price = assetDescription[currency];
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

        public static void MapFromAssetPrices<TSource, TDestination>(this IMemberConfigurationExpression<TSource, TDestination, IEnumerable<ItemPriceDTO>> memberOptions, Expression<Func<TSource, SteamAssetDescription>> assetDescriptionExpression)
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

                    return assetDescription.GetPrices(currency)
                        .OrderByDescending(x => x.Type == PriceType.SteamStore)
                        .ThenByDescending(x => x.Type == PriceType.SteamCommunityMarket)
                        .ThenByDescending(x => x.IsAvailable)
                        .ThenBy(x => x.LowestPrice)
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
