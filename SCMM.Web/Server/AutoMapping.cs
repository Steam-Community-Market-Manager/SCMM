using AutoMapper;
using SCMM.Web.Server.Domain.Models.Steam;
using SCMM.Web.Shared.Domain.DTOs.Steam;

namespace SCMM.Web.Server
{
    public class AutoMapping : Profile
    {
        public AutoMapping()
        {
            CreateMap<SteamCurrency, SteamCurrencyDTO>().ReverseMap();
            CreateMap<SteamLanguage, SteamLanguageDTO>().ReverseMap();
            CreateMap<SteamApp, SteamAppDTO>().ReverseMap();
            CreateMap<SteamItem, SteamItemDTO>().ReverseMap();
            CreateMap<SteamItemDescription, SteamItemDescriptionDTO>().ReverseMap();
        }
    }
}
