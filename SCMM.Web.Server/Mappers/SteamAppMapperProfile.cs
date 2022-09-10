using AutoMapper;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.App;

namespace SCMM.Web.Server.Mappers
{
    public class SteamAppMapperProfile : Profile
    {
        public SteamAppMapperProfile()
        {
            CreateMap<SteamApp, AppDTO>();
            CreateMap<SteamApp, AppListDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.SteamId));
            CreateMap<SteamApp, AppDetailedDTO>()
                .ForMember(x => x.Guid, o => o.MapFrom(p => p.Id))
                .ForMember(x => x.Id, o => o.MapFrom(p => p.SteamId));
        }
    }
}
