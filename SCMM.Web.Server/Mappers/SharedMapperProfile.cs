using AutoMapper;
using SCMM.Shared.Abstractions.WebProxies;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Web.Data.Models.UI.System;

namespace SCMM.Web.Server.Mappers
{
    public class SharedMapperProfile : Profile
    {
        public SharedMapperProfile()
        {
            CreateMap<WebProxyStatistic, SystemStatusWebProxyDTO>()
                .ForMember(x => x.Address, o => o.MapFrom(p => p.Address.MaskIpAddress()))
                .ForMember(x => x.CountryFlag, o => o.MapFrom(p => p.CountryCode.IsoCountryCodeToFlagEmoji()));
        }
    }
}
