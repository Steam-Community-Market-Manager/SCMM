using AutoMapper;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.Domain.Currencies;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SCMM.Web.Server.Extensions
{
    public static class AutoMapperConfigurationExtensions
    {
        public const string ContextKeyUser = "user";
        public const string ContextKeyLanguage = "language";
        public const string ContextKeyCurrency = "currency";

        public static void MapFromCurrency<TSource, TDestination>(this IMemberConfigurationExpression<TSource, TDestination, CurrencyDTO> memberOptions)
        {
            memberOptions.MapFrom((src, dst, _, context) =>
            {
                return context.Items.ContainsKey(ContextKeyCurrency)
                    ? (CurrencyDTO)context.Options.Items[ContextKeyCurrency]
                    : null;
            });
        }

        public static void MapFromUsingCurrencyExchange<TSource, TDestination>(this IMemberConfigurationExpression<TSource, TDestination, long> memberOptions, Expression<Func<TSource, long>> valueExpression, Expression<Func<TSource, SteamCurrency>> currencyExpression)
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

        public static void MapFromUsingCurrencyExchange<TSource, TDestination>(this IMemberConfigurationExpression<TSource, TDestination, long?> memberOptions, Expression<Func<TSource, long?>> valueExpression, Expression<Func<TSource, SteamCurrency>> currencyExpression)
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

    }
}
