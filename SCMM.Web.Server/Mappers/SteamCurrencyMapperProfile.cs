using AutoMapper;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Currency;
using SCMM.Web.Server;

namespace SCMM.Web.Server.Mappers
{
    public class SteamCurrencyMapperProfile : Profile
    {
        public SteamCurrencyMapperProfile()
        {
            CreateMap<SteamCurrency, CurrencyDTO>();
            CreateMap<SteamCurrency, CurrencyListDTO>();
            CreateMap<SteamCurrency, CurrencyDetailedDTO>();
        }
    }
}
