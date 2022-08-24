using AutoMapper;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Item;

namespace SCMM.Web.Server.Mappers
{
    public class SteamWorkshopFileMapperProfiler : Profile
    {
        public SteamWorkshopFileMapperProfiler()
        {
            CreateMap<SteamWorkshopFile, ItemDescriptionWorkshopFileDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.SteamId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.PreviewUrl))
                .ForMember(x => x.Actions, o => o.MapFrom(p => p.GetInteractions()));
        }
    }
}
