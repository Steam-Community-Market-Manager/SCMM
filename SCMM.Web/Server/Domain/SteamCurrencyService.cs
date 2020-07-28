using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SCMM.Web.Server.Data;
using SCMM.Web.Shared.Domain.DTOs.Currencies;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Domain
{
    public class SteamCurrencyService
    {
        private const string DefaultCacheKey = "default";

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
                .Select(x => _mapper.Map<CurrencyDetailedDTO>(x))
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.NeverRemove);

            Cache.Set(DefaultCacheKey, currencies.FirstOrDefault(x => x.IsDefault));
            foreach (var currency in currencies)
            {
                Cache.Set(currency.Name, currency, cacheOptions);
            }
        }

        public static CurrencyDetailedDTO GetDefaultCached()
        {
            CurrencyDetailedDTO value = null;
            Cache.TryGetValue(DefaultCacheKey, out value);
            return value;
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
