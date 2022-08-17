using AutoMapper;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Server.Extensions;

namespace SCMM.Web.Server.Mappers
{
    public class SteamMarketItemMapperProfile : Profile
    {
        public SteamMarketItemMapperProfile()
        {
            CreateMap<SteamMarketItem, ItemDescriptionWithPriceDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.Description.ClassId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.Description.App.SteamId))
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.ItemType, o => o.MapFrom(p => p.Description.ItemType))
                .ForMember(x => x.HasGlow, o => o.MapFrom(p => p.Description.HasGlow))
                .ForMember(x => x.DominantColour, o => o.MapFrom(p => p.Description.DominantColour))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconUrl))
                .ForMember(x => x.TimeAccepted, o => o.MapFrom(p => p.Description.TimeAccepted))
                .ForMember(x => x.BuyNowFrom, o => o.MapFromUsingAssetBuyPrice(p => p.Description, p => p.MarketType))
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingAssetBuyPrice(p => p.Description, p => p.Price))
                .ForMember(x => x.BuyNowUrl, o => o.MapFromUsingAssetBuyPrice(p => p.Description, p => p.Url))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.Description.SubscriptionsCurrent))
                .ForMember(x => x.SupplyTotalEstimated, o => o.MapFrom(p => p.Description.SupplyTotalEstimated))
                .ForMember(x => x.Actions, o => o.MapFrom(p => p.Description.GetInteractions()));

            CreateMap<SteamMarketItemOrder, ItemOrderDTO>()
                .ForMember(x => x.Price, o => o.MapFromUsingCurrencyExchange(p => p.Price, p => p.Item.Currency));
        }
    }
}
