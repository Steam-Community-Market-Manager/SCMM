using AutoMapper;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Language;
using SCMM.Web.Server;

namespace SCMM.Web.Server.Mappers
{
    public class SteamLanguageMapperProfile : Profile
    {
        public SteamLanguageMapperProfile()
        {
            CreateMap<SteamLanguage, LanguageDTO>();
            CreateMap<SteamLanguage, LanguageListDTO>();
            CreateMap<SteamLanguage, LanguageDetailedDTO>();
        }
    }
}
