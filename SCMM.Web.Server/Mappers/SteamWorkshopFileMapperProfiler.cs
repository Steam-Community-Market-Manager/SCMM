using AutoMapper;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Item;
using SCMM.Web.Data.Models.UI.Workshop;

namespace SCMM.Web.Server.Mappers
{
    public class SteamWorkshopFileMapperProfiler : Profile
    {
        public SteamWorkshopFileMapperProfiler()
        {
            CreateMap<SteamWorkshopFile, ItemDescriptionWithActionsDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.SteamId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.IconUrl, o => o.MapFrom(p => p.PreviewUrl))
                .ForMember(x => x.Actions, o => o.MapFrom(p => p.GetInteractions()));

            CreateMap<SteamWorkshopFile, WorkshopFileDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.SteamId))
                .ForMember(x => x.AppId, o => o.MapFrom(p => p.App.SteamId))
                .ForMember(x => x.CreatorId, o => o.MapFrom(p => p.CreatorProfile != null ? p.CreatorProfile.SteamId : p.App.SteamId))
                .ForMember(x => x.CreatorName, o => o.MapFrom(p => p.CreatorProfile != null ? p.CreatorProfile.Name : p.App.Name))
                .ForMember(x => x.CreatorAvatarUrl, o => o.MapFrom(p => p.CreatorProfile != null ? p.CreatorProfile.AvatarUrl : p.App.IconUrl))
                .ForMember(x => x.Actions, o => o.MapFrom(p => p.GetInteractions()));
        }
    }
}
