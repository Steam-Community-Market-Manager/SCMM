using AutoMapper;
using SCMM.Steam.Shared;
using SCMM.Web.Server.Domain.Models.Steam;
using SCMM.Web.Shared.Domain.DTOs.Steam;
using System.Linq;

namespace SCMM.Web.Server
{
    public class AutoMapping : Profile
    {
        public AutoMapping()
        {
            CreateMap<SteamCurrency, SteamCurrencyDTO>().ReverseMap();
            CreateMap<SteamLanguage, SteamLanguageDTO>().ReverseMap();
            CreateMap<SteamProfile, SteamProfileDTO>().ReverseMap();
            CreateMap<SteamApp, SteamAppDTO>().ReverseMap();
            CreateMap<SteamAssetFilter, SteamAssetFilterDTO>().ReverseMap();
            CreateMap<SteamStoreItem, SteamStoreItemDTO>().ReverseMap();
            CreateMap<SteamMarketItem, SteamMarketItemDTO>().ReverseMap();
            CreateMap<SteamMarketItemOrder, SteamMarketItemOrderDTO>().ReverseMap();
            CreateMap<SteamInventoryItem, SteamInventoryItemDTO>().ReverseMap();
            CreateMap<SteamAssetDescription, SteamAssetDescriptionDTO>()
                .ForMember(x => x.Tags, o => o.MapFrom(p => p.Tags.Where(x => !x.Key.StartsWith(SteamConstants.SteamAssetTagWorkshop)))) // ignore workshop skins
                .ReverseMap();
            CreateMap<SteamAssetWorkshopFile, SteamAssetWorkshopFileDTO>()
                .ForMember(x => x.SubscriptionsGraph, o => o.MapFrom(p => p.SubscriptionsGraph.ToDictionary(x => x.Key.ToString("dd MMM yyyy"), x => x.Value)))
                .ReverseMap();
        }
    }
}
