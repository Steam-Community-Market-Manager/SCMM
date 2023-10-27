using AutoMapper;
using SCMM.Shared.Abstractions.WebProxies;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Models.Statistics;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Web.Data.Models.UI.System;

namespace SCMM.Web.Server.Mappers
{
    public class SharedMapperProfile : Profile
    {
        public SharedMapperProfile()
        {
            CreateMap<KeyValuePair<MarketType, MarketStatusStatistic>, SystemStatusAppMarketDTO>()
                .ForMember(x => x.Type, o => o.MapFrom(p => p.Key))
                .ForMember(x => x.TotalItems, o => o.MapFrom(p => p.Value.TotalItems))
                .ForMember(x => x.TotalListings, o => o.MapFrom(p => p.Value.TotalListings))
                .ForMember(x => x.LastUpdatedItemsOn, o => o.MapFrom(p => p.Value.LastUpdatedItemsOn))
                .ForMember(x => x.LastUpdatedItemsDuration, o => o.MapFrom(p => p.Value.LastUpdatedItemsDuration))
                .ForMember(x => x.LastUpdateErrorOn, o => o.MapFrom(p => p.Value.LastUpdateErrorOn))
                .ForMember(x => x.LastUpdateError, o => o.MapFrom(p => p.Value.LastUpdateError));

            CreateMap<WebProxyWithUsageStatistics, SystemStatusWebProxyDTO>()
                .ForMember(x => x.Address, o => o.MapFrom(p => p.Address.MaskIpAddress()))
                .ForMember(x => x.CountryFlag, o => o.MapFrom(p => p.CountryCode.IsoCountryCodeToFlagEmoji()));
        }
    }
}
