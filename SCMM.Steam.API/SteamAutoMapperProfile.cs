using AutoMapper;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Models.Domain.Currencies;
using SCMM.Steam.Data.Models.Domain.Languages;

namespace SCMM.Steam.API
{
    public class SteamAutoMapperProfile : Profile
    {
        public SteamAutoMapperProfile()
        {
            CreateMap<SteamLanguage, LanguageDTO>();
            CreateMap<SteamLanguage, LanguageListDTO>();
            CreateMap<SteamLanguage, LanguageDetailedDTO>();

            CreateMap<SteamCurrency, CurrencyDTO>();
            CreateMap<SteamCurrency, CurrencyListDTO>();
            CreateMap<SteamCurrency, CurrencyDetailedDTO>();
        }
    }
}
