using AutoMapper;
using Microsoft.AspNetCore.Http;
using SCMM.Web.Server.API.Controllers.Extensions;
using SCMM.Web.Server.Domain.Models.Steam;
using SCMM.Web.Shared;
using SCMM.Web.Shared.Domain.DTOs;
using SCMM.Web.Shared.Domain.DTOs.Currencies;
using SCMM.Web.Shared.Domain.DTOs.InventoryItems;
using SCMM.Web.Shared.Domain.DTOs.Languages;
using SCMM.Web.Shared.Domain.DTOs.MarketItems;
using SCMM.Web.Shared.Domain.DTOs.Profiles;
using SCMM.Web.Shared.Domain.DTOs.StoreItems;
using System;
using System.Linq.Expressions;

namespace SCMM.Web.Server
{
    public class AutoMapping : Profile
    {
        public AutoMapping()
        {
            CreateMap<SteamLanguage, LanguageDTO>();
            CreateMap<SteamLanguage, LanguageListDTO>();
            CreateMap<SteamLanguage, LanguageDetailedDTO>();

            CreateMap<SteamCurrency, CurrencyDTO>();
            CreateMap<SteamCurrency, CurrencyListDTO>()
                .ForMember(x => x.Symbol, o => o.MapFrom(p => p.PrefixText));
            CreateMap<SteamCurrency, CurrencyDetailedDTO>();

            CreateMap<SteamProfile, ProfileDTO>();
            CreateMap<SteamProfile, ProfileDetailedDTO>();
            CreateMap<SteamProfile, ProfileInventoryDetailsDTO>();

            CreateMap<SteamInventoryItem, InventoryItemListDTO>()
                .ForMember(x => x.SteamAppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconUrl))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.BuyPrice, o => o.MapFromUsingCurrencyExchange(p => p.BuyPrice, p => p.Currency));

            CreateMap<SteamMarketItem, InventoryMarketItemDTO>()
                .ForMember(x => x.SteamAppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconUrl))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.BuyAskingPrice, o => o.MapFromUsingCurrencyExchange(p => p.BuyAskingPrice, p => p.Currency))
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingCurrencyExchange(p => p.BuyNowPrice, p => p.Currency))
                .ForMember(x => x.ResellPrice, o => o.MapFromUsingCurrencyExchange(p => p.ResellPrice, p => p.Currency))
                .ForMember(x => x.ResellTax, o => o.MapFromUsingCurrencyExchange(p => p.ResellTax, p => p.Currency))
                .ForMember(x => x.ResellProfit, o => o.MapFromUsingCurrencyExchange(p => p.ResellProfit, p => p.Currency))
                .ForMember(x => x.Last1hrValue, o => o.MapFromUsingCurrencyExchange(p => p.Last1hrValue, p => p.Currency));

            CreateMap<SteamMarketItem, MarketItemListDTO>()
                .ForMember(x => x.SteamAppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconUrl))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.BuyAskingPrice, o => o.MapFromUsingCurrencyExchange(p => p.BuyAskingPrice, p => p.Currency))
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingCurrencyExchange(p => p.BuyNowPrice, p => p.Currency))
                .ForMember(x => x.ResellPrice, o => o.MapFromUsingCurrencyExchange(p => p.ResellPrice, p => p.Currency))
                .ForMember(x => x.ResellTax, o => o.MapFromUsingCurrencyExchange(p => p.ResellTax, p => p.Currency))
                .ForMember(x => x.ResellProfit, o => o.MapFromUsingCurrencyExchange(p => p.ResellProfit, p => p.Currency))
                .ForMember(x => x.First24hrValue, o => o.MapFromUsingCurrencyExchange(p => p.First24hrValue, p => p.Currency))
                .ForMember(x => x.Last1hrValue, o => o.MapFromUsingCurrencyExchange(p => p.Last1hrValue, p => p.Currency))
                .ForMember(x => x.Last24hrValue, o => o.MapFromUsingCurrencyExchange(p => p.Last24hrValue, p => p.Currency))
                .ForMember(x => x.Last48hrValue, o => o.MapFromUsingCurrencyExchange(p => p.Last48hrValue, p => p.Currency))
                .ForMember(x => x.Last72hrValue, o => o.MapFromUsingCurrencyExchange(p => p.Last72hrValue, p => p.Currency))
                .ForMember(x => x.Last120hrValue, o => o.MapFromUsingCurrencyExchange(p => p.Last120hrValue, p => p.Currency))
                .ForMember(x => x.Last336hrValue, o => o.MapFromUsingCurrencyExchange(p => p.Last336hrValue, p => p.Currency))
                .ForMember(x => x.MovementLast48hrValue, o => o.MapFromUsingCurrencyExchange(p => p.MovementLast48hrValue, p => p.Currency))
                .ForMember(x => x.MovementLast120hrValue, o => o.MapFromUsingCurrencyExchange(p => p.MovementLast120hrValue, p => p.Currency))
                .ForMember(x => x.MovementLast336hrValue, o => o.MapFromUsingCurrencyExchange(p => p.MovementLast336hrValue, p => p.Currency))
                .ForMember(x => x.AllTimeAverageValue, o => o.MapFromUsingCurrencyExchange(p => p.AllTimeAverageValue, p => p.Currency))
                .ForMember(x => x.AllTimeHighestValue, o => o.MapFromUsingCurrencyExchange(p => p.AllTimeHighestValue, p => p.Currency))
                .ForMember(x => x.AllTimeLowestValue, o => o.MapFromUsingCurrencyExchange(p => p.AllTimeLowestValue, p => p.Currency))
                .ForMember(x => x.MarketAge, o => o.MapFrom(p => p.MarketAge.ToMarketAgeString()))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.Description.WorkshopFile.Subscriptions))
                .ForMember(x => x.Tags, o => o.MapFrom(p => p.Description.Tags.WithoutWorkshopTags()));

            CreateMap<SteamMarketItem, MarketItemDetailDTO>()
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconLargeUrl))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.Description.WorkshopFile.Subscriptions))
                .ForMember(x => x.Favourited, o => o.MapFrom(p => p.Description.WorkshopFile.Favourited))
                .ForMember(x => x.Views, o => o.MapFrom(p => p.Description.WorkshopFile.Views))
                .ForMember(x => x.Tags, o => o.MapFrom(p => p.Description.Tags.WithoutWorkshopTags()));
            CreateMap<SteamMarketItemActivity, MarketItemActivityDTO>()
                .ForMember(x => x.Movement, o => o.MapFromUsingCurrencyExchange(p => p.Movement, p => p.Item.Currency));
            CreateMap<SteamMarketItemOrder, MarketItemOrderDTO>()
                .ForMember(x => x.Price, o => o.MapFromUsingCurrencyExchange(p => p.Price, p => p.Item.Currency));
            CreateMap<SteamMarketItemSale, MarketItemSaleDTO>()
                .ForMember(x => x.Price, o => o.MapFromUsingCurrencyExchange(p => p.Price, p => p.Item.Currency));

            CreateMap<SteamStoreItem, StoreItemListDTO>()
                .ForMember(x => x.SteamAppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.SteamWorkshopId, o => o.MapFrom(p => p.Description.WorkshopFile.SteamId))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconUrl))
                .ForMember(x => x.AuthorName, o => o.MapFrom(p => p.Description.WorkshopFile.Creator.Name))
                .ForMember(x => x.ItemType, o => o.MapFrom(p => p.Description.Tags.GetItemType(p.Description.Name)))
                .ForMember(x => x.Currency, o => o.MapFromCurrency())
                .ForMember(x => x.StorePrice, o => o.MapFrom(
                    (src, dst, _, context) =>
                    {
                        return context.Items.ContainsKey(AutoMappingExtensions.ContextKeyCurrencyId)
                            ? src.StorePrices[(string)context.Options.Items[AutoMappingExtensions.ContextKeyCurrencyId]]
                            : 0;
                    }
                ))
                .ForMember(x => x.StoreRankHistory, o => o.MapFrom(p => p.StoreRankGraph.ToGraphDictionary()))
                .ForMember(x => x.TotalSalesHistory, o => o.MapFrom(p => p.TotalSalesGraph.ToGraphDictionary()))
                .ForMember(x => x.SubscriptionsHistory, o => o.MapFrom(p => p.Description.WorkshopFile.SubscriptionsGraph.ToGraphDictionary()))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.Description.WorkshopFile.Subscriptions))
                .ForMember(x => x.Favourited, o => o.MapFrom(p => p.Description.WorkshopFile.Favourited))
                .ForMember(x => x.Views, o => o.MapFrom(p => p.Description.WorkshopFile.Views))
                .ForMember(x => x.AcceptedOn, o => o.MapFrom(p => p.Description.WorkshopFile.AcceptedOn))
                .ForMember(x => x.Tags, o => o.MapFrom(p => p.Description.Tags.WithoutWorkshopTags()));
        }
    }

    public static class AutoMappingExtensions
    {
        public const string ContextKeyLanguageId = "languageId";
        public const string ContextKeyLanguage = "language";
        public const string ContextKeyCurrencyId = "currencyId";
        public const string ContextKeyCurrency = "currency";
        public const string ContextKeyProfileId = "profileId";

        public static IMappingOperationOptions AddRequest(this IMappingOperationOptions opt, HttpRequest request)
        {
            opt.Items[ContextKeyLanguageId] = request.LanguageId();
            opt.Items[ContextKeyLanguage] = request.Language();
            opt.Items[ContextKeyCurrencyId] = request.CurrencyId();
            opt.Items[ContextKeyCurrency] = request.Currency();
            opt.Items[ContextKeyProfileId] = request.ProfileId();
            return opt;
        }

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
                    return (long?) null;
                }

                var value = valueExpression.Compile().Invoke(src);
                if (value == null)
                {
                    return (long?) null;
                }

                var valueCurrency = currencyExpression.Compile().Invoke(src);
                if (valueCurrency == null)
                {
                    return (long?) null;
                }

                var targetCurrency = (CurrencyDetailedDTO)context.Items[ContextKeyCurrency];
                return targetCurrency.CalculateExchange(value.Value, valueCurrency);
            });
        }
    }
}
