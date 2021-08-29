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
                .ForMember(x => x.Name, o => o.MapFrom(p => p.Description.Name))
                .ForMember(x => x.BackgroundColour, o => o.MapFrom(p => p.Description.BackgroundColour))
                .ForMember(x => x.ForegroundColour, o => o.MapFrom(p => p.Description.ForegroundColour))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.Description.IconUrl))
                .ForMember(x => x.BuyNowFrom, o => o.MapFromUsingAssetPrice(p => p.Description, p => p.Type))
                .ForMember(x => x.BuyNowPrice, o => o.MapFromUsingAssetPrice(p => p.Description, p => p.LowestPrice))
                .ForMember(x => x.BuyNowUrl, o => o.MapFromUsingAssetPrice(p => p.Description, p => p.Url))
                .ForMember(x => x.Subscriptions, o => o.MapFrom(p => p.Description.LifetimeSubscriptions));

            CreateMap<SteamMarketItemOrder, ItemOrderDTO>()
                .ForMember(x => x.Price, o => o.MapFromUsingCurrencyExchange(p => p.Price, p => p.Item.Currency));
        }
    }
}
