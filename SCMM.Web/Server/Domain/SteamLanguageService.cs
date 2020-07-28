using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SCMM.Web.Server.Data;
using SCMM.Web.Shared.Domain.DTOs.Languages;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Domain
{
    public class SteamLanguageService
    {
        private const string DefaultCacheKey = "default";

        private static IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        private readonly SteamDbContext _db;
        private readonly IMapper _mapper;

        public SteamLanguageService(SteamDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task RepopulateCache()
        {
            var languages = await _db.SteamLanguages
                .Select(x => _mapper.Map<LanguageDetailedDTO>(x))
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.NeverRemove);

            Cache.Set(DefaultCacheKey, languages.FirstOrDefault(x => x.IsDefault));
            foreach (var language in languages)
            {
                Cache.Set(language.Name, language, cacheOptions);
            }
        }

        public static LanguageDetailedDTO GetDefaultCached()
        {
            LanguageDetailedDTO value = null;
            Cache.TryGetValue(DefaultCacheKey, out value);
            return value;
        }

        public static LanguageDetailedDTO GetByNameCached(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return null;
            }

            LanguageDetailedDTO value = null;
            Cache.TryGetValue(name, out value);
            return value;
        }
    }
}
