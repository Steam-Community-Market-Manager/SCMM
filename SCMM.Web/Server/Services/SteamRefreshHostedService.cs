using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services
{
    public class SteamRefreshHostedService : CronJobService
    {
        private readonly ILogger<SteamRefreshHostedService> _logger;
        private readonly SteamClient _steamClient;

        public SteamRefreshHostedService(IConfiguration configuration, ILogger<SteamRefreshHostedService> logger) 
            : base(configuration[$"Jobs:{nameof(SteamRefreshHostedService)}"])
        {
            _logger = logger;
            _steamClient = new SteamClient();
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            /*
            _logger.LogInformation($"Refreshing market items for '{app.App.Id}'...");
            var fetched = 0;
            var total = await _steamClient.GetMarketSearchPaginated(request);
            if (total == 0)
                return;

            _logger.LogInformation($"There are {total} market items to check for '{app.App.Id}'");
            for (var i = 0; i <= total; i += PaginatedSearchPageSize)
            {
                var start = i;
                var count = Math.Min(total - i, PaginatedSearchPageSize);
                _steamClient.GetMarketSearchPaginated(request).ToObservable()
                    .Where(x => x != null)
                    .ObserveOnUIThread()
                    .Subscribe(x =>
                    {
                        fetched += x.Count();
                        _logger.LogInformation($"Refreshing market items ({fetched}/{total})");
                        foreach (var item in x)
                        {
                            FindOrAddMarketItem(profile, app, item.AssetDescription);
                        }
                    });
            }
            */
        }
    }
}
