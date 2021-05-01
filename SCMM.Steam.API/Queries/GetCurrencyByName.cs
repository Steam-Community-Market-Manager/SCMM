using AutoMapper;
using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Data.Store;
using System;
using System.Linq;
using System.Threading.Tasks;
using SCMM.Shared.Data.Models.Extensions;

namespace SCMM.Steam.API.Queries
{
    public class GetCurrencyByNameRequest : IQuery<GetCurrencyByNameResponse>
    {
        public string Name { get; set; }
    }

    public class GetCurrencyByNameResponse
    {
        public SteamCurrency Currency { get; set; }
    }

    public class GetCurrencyByName : IQueryHandler<GetCurrencyByNameRequest, GetCurrencyByNameResponse>
    {
        private readonly SteamDbContext _db;

        public GetCurrencyByName(SteamDbContext db)
        {
            _db = db;
        }

        public async Task<GetCurrencyByNameResponse> HandleAsync(GetCurrencyByNameRequest request)
        {
            var currency = (SteamCurrency)null;
            var currencies = await _db.SteamCurrencies.AsNoTracking().ToListAsync();
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
                Currency = currency
            };
        }
    }
}
