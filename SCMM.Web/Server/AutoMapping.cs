using AutoMapper;
using SCMM.Web.Server.Domain.Models.Steam;
using SCMM.Web.Shared;
using SCMM.Web.Shared.Domain.DTOs;
using SCMM.Web.Shared.Domain.DTOs.MarketItems;
using SCMM.Web.Shared.Domain.DTOs.StoreItems;

namespace SCMM.Web.Server
{
    public class AutoMapping : Profile
    {
        public AutoMapping()
        {
            CreateMap<SteamCurrency, CurrencyDTO>();
            CreateMap<SteamProfile, ProfileDTO>();

            CreateMap<SteamMarketItem, MarketItemListDTO>()
                .ForMember(x => x.SteamAppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconUrl))
                .ForMember(x => x.MarketAge, o => o.MapFrom(p => p.MarketAge.ToMarketAgeString()))
                .ForMember(x => x.DemandUnique, o => o.MapFrom(p => p.BuyOrders.Count))
                .ForMember(x => x.SupplyUnique, o => o.MapFrom(p => p.SellOrders.Count))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.Description.WorkshopFile.Subscriptions))
                .ForMember(x => x.Tags, o => o.MapFrom(p => p.Description.Tags.WithoutWorkshopTags()));

            CreateMap<SteamMarketItemSale, MarketItemSaleDTO>();
            CreateMap<SteamMarketItemOrder, MarketItemOrderDTO>();
            CreateMap<SteamMarketItem, MarketItemDetailDTO>()
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconLargeUrl))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.Description.WorkshopFile.Subscriptions))
                .ForMember(x => x.Favourited, o => o.MapFrom(p => p.Description.WorkshopFile.Favourited))
                .ForMember(x => x.Views, o => o.MapFrom(p => p.Description.WorkshopFile.Views))
                .ForMember(x => x.Tags, o => o.MapFrom(p => p.Description.Tags.WithoutWorkshopTags()));

            CreateMap<SteamStoreItem, StoreItemListDTO>()
                .ForMember(x => x.SteamAppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.SteamWorkshopId, o => o.MapFrom(p => p.Description.WorkshopFile.SteamId))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconUrl))
                .ForMember(x => x.AcceptedOn, o => o.MapFrom(p => p.Description.WorkshopFile.AcceptedOn))
                .ForMember(x => x.SubscriptionsHistory, o => o.MapFrom(p => p.Description.WorkshopFile.SubscriptionsGraph.ToGraphDictionary()))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.Description.WorkshopFile.Subscriptions))
                .ForMember(x => x.Favourited, o => o.MapFrom(p => p.Description.WorkshopFile.Favourited))
                .ForMember(x => x.Views, o => o.MapFrom(p => p.Description.WorkshopFile.Views))
                .ForMember(x => x.Tags, o => o.MapFrom(p => p.Description.Tags.WithoutWorkshopTags()));
        }
    }
}
