using AutoMapper;
using SCMM.Web.Server.Data.Models.Steam;
using SCMM.Web.Shared;
using SCMM.Web.Shared.Domain.DTOs.Currencies;
using System;
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
            });
        }

        public static void MapFromUsingCurrencyExchange<TSource, TDestination>(this IMemberConfigurationExpression<TSource, TDestination, long?> memberOptions, Expression<Func<TSource, long?>> valueExpression, Expression<Func<TSource, SteamCurrency>> currencyExpression)
        {
            memberOptions.MapFrom((src, dst, _, context) =>
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
            });
        }
    }
}
