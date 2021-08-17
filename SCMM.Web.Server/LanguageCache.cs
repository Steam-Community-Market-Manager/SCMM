using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.Language;

namespace SCMM.Web.Server
{
    public class LanguageCache
    {
        private static readonly IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        private readonly SteamDbContext _db;
        private readonly IMapper _mapper;

        public LanguageCache(SteamDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public void RepopulateCache()
        {
            lock (Cache)
            {
                var languages = _db.SteamLanguages
                    .AsNoTracking()
                    .Select(x => _mapper.Map<LanguageDetailedDTO>(x))
                    .ToList();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetPriority(CacheItemPriority.NeverRemove);

                foreach (var language in languages)
                {
                    Cache.Set(language.Name.Trim().ToUpper(), language, cacheOptions);
                }
            }
        }

        public static LanguageDetailedDTO GetByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            Cache.TryGetValue(name.Trim().ToUpper(), out
            LanguageDetailedDTO value);
            return value;
        }
    }
}
