using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.Domain.DTOs.Languages;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services
{
    public class SteamLanguageService
    {
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
                .AsNoTracking()
                .Select(x => _mapper.Map<LanguageDetailedDTO>(x))
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.NeverRemove);

            foreach (var language in languages)
            {
                Cache.Set(language.Name, language, cacheOptions);
            }
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
