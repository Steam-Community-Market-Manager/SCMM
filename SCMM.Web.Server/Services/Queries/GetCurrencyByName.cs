using AutoMapper;
using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Data.Shared.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Models.Steam;
using SCMM.Web.Data.Models.Domain.DTOs.Currencies;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Queries
{
    public class GetCurrencyByNameRequest : IQuery<GetCurrencyByNameResponse>
    {
        public string Name { get; set; }
    }

    public class GetCurrencyByNameResponse
    {
        public CurrencyDetailedDTO Currency { get; set; }
    }

    public class GetCurrencyByName : IQueryHandler<GetCurrencyByNameRequest, GetCurrencyByNameResponse>
    {
        private readonly SteamDbContext _db;
        private readonly IMapper _mapper;

        public GetCurrencyByName(SteamDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<GetCurrencyByNameResponse> HandleAsync(GetCurrencyByNameRequest request)
        {
            var currency = (SteamCurrency)null;
            var currencies = _db.SteamCurrencies.AsNoTracking().ToList();
            if (!String.IsNullOrEmpty(request.Name))
            {
                currency = currencies.Closest(x => x.Name, request.Name);
            }
            else
            {
                currency = currencies.FirstOrDefault(x => x.IsDefault);
            }

            return new GetCurrencyByNameResponse
            {
                Currency = _mapper.Map<CurrencyDetailedDTO>(currency)
            };
        }
    }
}
