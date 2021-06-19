using AutoMapper;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Data.Models.UI.Price;
using SCMM.Web.Server.Extensions;
using System.Collections.Generic;
using System.Linq;
using SCMM.Web.Server;

namespace SCMM.Web.Server.Mappers
{
    public class SteamAssetDescriptionMapperProfile : Profile
    {
        public SteamAssetDescriptionMapperProfile()
        {
            CreateMap<Price, PriceDTO>();

            CreateMap<SteamAssetDescription, ItemDetailedDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.ClassId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.Creator, o => o.MapFrom(p => p.Creator))
                .ForMember(x => x.MarketId, o => o.MapFrom(p => p.NameHash))
                .ForMember(x => x.MarketBuyOrderCount, o => o.MapFrom(p => (p.MarketItem != null ? (int?)p.MarketItem.Demand : null)))
                .ForMember(x => x.MarketBuyPrice, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.BuyNowPrice : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.MarketSellOrderCount, o => o.MapFrom(p => (p.MarketItem != null ? (int?)p.MarketItem.Supply : null)))
                .ForMember(x => x.MarketSellPrice, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.BuyAskingPrice : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.MarketSellTax, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.ResellTax : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.Market1hrSales, o => o.MapFrom(p => (p.MarketItem != null ? (long?)p.MarketItem.Last1hrSales : null)))
                .ForMember(x => x.Market1hrValue, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.Last1hrValue : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.Market24hrSales, o => o.MapFrom(p => (p.MarketItem != null ? (long?)p.MarketItem.Last24hrSales : null)))
                .ForMember(x => x.Market24hrValue, o => o.MapFromUsingCurrencyExchange(p => (p.MarketItem != null ? (long?)p.MarketItem.Last24hrValue : null), p => (p.MarketItem != null ? p.MarketItem.Currency : null)))
                .ForMember(x => x.IsAvailableOnMarket, o => o.MapFrom(p => (p.MarketItem != null ? true : false)))
                .ForMember(x => x.StoreId, o => o.MapFrom(p => (p.StoreItem != null ? p.StoreItem.SteamId : null)))
                .ForMember(x => x.StorePrice, o => o.MapFromUsingCurrencyExchange(p => (p.StoreItem != null ? (int?)p.StoreItem.Price : null), p => (p.StoreItem != null ? p.StoreItem.Currency : null)))
                .ForMember(x => x.IsAvailableOnStore, o => o.MapFrom(p => (p.StoreItem != null ? true : false)));

            CreateMap<List<SteamAssetDescription>, ItemCollectionDTO>()
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Count > 0 ? p.FirstOrDefault().ItemCollection : null))
                .ForMember(x => x.AuthorName, o => o.MapFrom(p => p.Count(x => x.Creator != null) > 0 ? p.FirstOrDefault(x => x.Creator != null).Creator.Name : null))
                .ForMember(x => x.AuthorAvatarUrl, o => o.MapFrom(p => p.Count(x => x.Creator != null) > 0 ? p.FirstOrDefault(x => x.Creator != null).Creator.AvatarUrl : null))
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingCurrencyExchange(p => p.Count > 0 ? p.Sum(x => x.BuyNowPrice ?? 0) : null, p => p.Count > 0 ? p.FirstOrDefault().BuyNowCurrency : null))
                .ForMember(x => x.Items, o => o.MapFrom(p => p));

            CreateMap<SteamAssetDescription, ItemDescriptionDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.ClassId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.App.SteamId));

            CreateMap<SteamAssetDescription, ItemDescriptionWithPriceDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.ClassId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingCurrencyExchange(p => p.BuyNowPrice, p => p.BuyNowCurrency))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.CurrentSubscriptions));
        }
    }
}
