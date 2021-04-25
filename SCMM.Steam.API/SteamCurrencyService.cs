using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SCMM.Steam.Data.Store;
using System;
using System.Linq;
using System.Threading.Tasks;
using SCMM.Steam.Data.Models.Domain.Currencies;

namespace SCMM.Steam.API
{
    public class SteamCurrencyService
    {
        private static IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        private readonly SteamDbContext _db;
        private readonly IMapper _mapper;

        public SteamCurrencyService(SteamDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task RepopulateCache()
        {
            var currencies = await _db.SteamCurrencies
                .AsNoTracking()
                .Select(x => _mapper.Map<CurrencyDetailedDTO>(x))
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.NeverRemove);

            foreach (var currency in currencies)
            {
                Cache.Set(currency.Name, currency, cacheOptions);
            }
        }

        public static CurrencyDetailedDTO GetByNameCached(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return null;
            }

            CurrencyDetailedDTO value = null;
            Cache.TryGetValue(name, out value);
            return value;
        }
    }
}
