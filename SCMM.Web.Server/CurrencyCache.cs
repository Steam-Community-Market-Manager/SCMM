using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Currency;
using System.Linq;

namespace SCMM.Web.Server
{
    public class CurrencyCache
    {
        private static IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        private readonly SteamDbContext _db;
        private readonly IMapper _mapper;

        public CurrencyCache(SteamDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public void RepopulateCache()
        {
            lock (Cache)
            {
                var currencies = _db.SteamCurrencies
                    .AsNoTracking()
                    .Select(x => _mapper.Map<CurrencyDetailedDTO>(x))
                    .ToList();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetPriority(CacheItemPriority.NeverRemove);

                foreach (var currency in currencies)
                {
                    Cache.Set(currency.Name, currency, cacheOptions);
                }
            }
        }

        public void UpdateExchangeRate(string currency, decimal exchangeRateMultiplier)
        {
            var cached = GetByName(currency);
            if (cached != null)
            {
                lock (Cache)
                {
                    cached.ExchangeRateMultiplier = exchangeRateMultiplier;
                    Cache.Set(cached.Name, cached);
                }
            }
        }

        public static CurrencyDetailedDTO GetByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            CurrencyDetailedDTO value = null;
            Cache.TryGetValue(name, out value);
            return value;
        }
    }
}
