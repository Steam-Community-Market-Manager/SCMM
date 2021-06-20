using AutoMapper;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Currency;

namespace SCMM.Web.Server.Mappers
{
    public class SteamCurrencyMapperProfile : Profile
    {
        public SteamCurrencyMapperProfile()
        {
            CreateMap<SteamCurrency, CurrencyDTO>();
            CreateMap<SteamCurrency, CurrencyListDTO>()
                .ForMember(x => x.Id, o => o.MapFrom(p => p.SteamId));
            CreateMap<SteamCurrency, CurrencyDetailedDTO>()
                .ForMember(x => x.Guid, o => o.MapFrom(p => p.Id))
                .ForMember(x => x.Id, o => o.MapFrom(p => p.SteamId));
        }
    }
}
