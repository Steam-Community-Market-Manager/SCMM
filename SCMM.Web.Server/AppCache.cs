using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.App;

namespace SCMM.Web.Server
{
    public class AppCache
    {
        private static readonly IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        private readonly SteamDbContext _db;
        private readonly IMapper _mapper;

        public AppCache(SteamDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public void RepopulateCache()
        {
            lock (Cache)
            {
                var apps = _db.SteamApps
                    .AsNoTracking()
                    .Select(x => _mapper.Map<AppDetailedDTO>(x))
                    .ToList();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetPriority(CacheItemPriority.NeverRemove);

                foreach (var app in apps)
                {
                    Cache.Set(app.Id, app, cacheOptions);
                    if (!string.IsNullOrEmpty(app.Subdomain))
                    {
                        Cache.Set(app.Subdomain, app, cacheOptions);
                    }
                }
            }
        }

        public static AppDetailedDTO GetById(ulong id)
        {
            if (id <= 0)
            {
                return null;
            }

            Cache.TryGetValue(id, out AppDetailedDTO app);
            return app;
        }

        public static AppDetailedDTO GetByHostname(string hostname)
        {
            if (string.IsNullOrEmpty(hostname))
            {
                return null;
            }

            var hostnameParts = hostname?.Split('.');
            foreach (var part in hostnameParts)
            {
                if (Cache.TryGetValue(part, out AppDetailedDTO app))
                {
                    return app;
                }
            }

            return null;
        }
    }
}
