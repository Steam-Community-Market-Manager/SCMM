using AutoMapper;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Language;

namespace SCMM.Web.Server.Mappers
{
    public class SteamLanguageMapperProfile : Profile
    {
        public SteamLanguageMapperProfile()
        {
            CreateMap<SteamLanguage, LanguageDTO>();
            CreateMap<SteamLanguage, LanguageListDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.SteamId));
            CreateMap<SteamLanguage, LanguageDetailedDTO>()
                .ForMember(x => x.Guid, o => o.MapFrom(p => p.Id))
                .ForMember(x => x.Id, o => o.MapFrom(p => p.SteamId));
        }
    }
}
