using AutoMapper;
using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Abstractions.Statistics;
using SCMM.Shared.Abstractions.WebProxies;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Models.Statistics;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.System;

namespace SCMM.Web.Server.Queries
{
    public class GetSystemStatusRequest : IQuery<GetSystemStatusResponse>
    {
        public ulong AppId { get; set; }

        public bool IncludeAppStatus { get; set; } = false;

        public bool IncludeMarketStatus { get; set; } = false;

        public bool IncludeWebProxyStatus { get; set; } = false;

        public bool IncludeAlerts { get; set; } = true;
    }

    public class GetSystemStatusResponse
    {
        public SystemStatusDTO Status { get; set; }
    }

    public class GetSystemStatus : IQueryHandler<GetSystemStatusRequest, GetSystemStatusResponse>
    {
        private readonly SteamDbContext _db;
        private readonly IStatisticsService _statisticsService;
        private readonly IWebProxyUsageStatisticsService _webProxyStatisticsService;
        private readonly IMapper _mapper;

        public GetSystemStatus(SteamDbContext db, IStatisticsService statisticsService, IWebProxyUsageStatisticsService webProxyStatisticsService, IMapper mapper)
        {
            _db = db;
            _statisticsService = statisticsService;
            _webProxyStatisticsService = webProxyStatisticsService;
            _mapper = mapper;
        }

        public async Task<GetSystemStatusResponse> HandleAsync(GetSystemStatusRequest request)
        {
            var steamApp = (SystemStatusSteamAppDTO)null;
            if (request.IncludeAppStatus)
            {
                steamApp = await _db.SteamApps
                    .AsNoTracking()
                    .Where(x => x.SteamId == request.AppId.ToString())
                    .Select(x => new SystemStatusSteamAppDTO()
                    {
                        Id = x.SteamId,
                        Name = x.Name,
                        IconUrl = x.IconUrl,
                        ItemDefinitionArchives = x.ItemDefinitionArchives
                            .OrderByDescending(a => a.TimePublished)
                            .Take(5)
                            .Select(a => new SystemStatusAppItemDefinitionArchive
                            {
                                Digest = a.Digest,
                                Size = a.ItemDefinitionsSize,
                                PublishedOn = a.TimePublished,
                                IsImported = true
                            })
                            .ToArray(),
                        AssetDescriptionsUpdates = new TimeRangeWithTargetDTO()
                        {
                            Oldest = x.AssetDescriptions.Where(x => x.ClassId != null).Min(y => y.TimeRefreshed),
                            Newest = x.AssetDescriptions.Where(x => x.ClassId != null).Max(y => y.TimeRefreshed)
                        },
                        MarketOrderUpdates = new TimeRangeWithTargetDTO()
                        {
                            Oldest = x.MarketItems.Where(x => x.Description.IsMarketable).Min(y => y.LastCheckedOrdersOn),
                            Newest = x.MarketItems.Where(x => x.Description.IsMarketable).Max(y => y.LastCheckedOrdersOn)
                        },
                        MarketSaleUpdates = new TimeRangeWithTargetDTO()
                        {
                            Oldest = x.MarketItems.Where(x => x.Description.IsMarketable).Min(y => y.LastCheckedSalesOn),
                            Newest = x.MarketItems.Where(x => x.Description.IsMarketable).Max(y => y.LastCheckedSalesOn)
                        },
                        MarketActivityUpdates = new TimeRangeWithTargetDTO()
                        {
                            Oldest = x.MarketItems.Where(x => x.Description.IsMarketable).Min(y => y.LastCheckedActivityOn),
                            Newest = x.MarketItems.Where(x => x.Description.IsMarketable).Max(y => y.LastCheckedActivityOn)
                        }
                    })
                    .FirstOrDefaultAsync();

                if (steamApp != null)
                {
                    // TODO: Make these configurable
                    steamApp.AssetDescriptionsUpdates.TargetDelta = TimeSpan.FromDays(1);
                    steamApp.MarketOrderUpdates.TargetDelta = TimeSpan.FromHours(3);
                    steamApp.MarketSaleUpdates.TargetDelta = TimeSpan.FromHours(3);
                    steamApp.MarketActivityUpdates.TargetDelta = TimeSpan.FromMinutes(10);
                }
            }

            var markets = (IEnumerable<SystemStatusAppMarketDTO>)null;
            if (request.IncludeMarketStatus)
            {
                // TODO: Add target "last updated" time for markets
                var marketStats = _mapper.Map<IEnumerable<SystemStatusAppMarketDTO>>(
                    await _statisticsService.GetDictionaryAsync<MarketType, MarketStatusStatistic>(
                        String.Format(StatisticKeys.MarketStatusByAppId, request.AppId.ToString())
                    )
                );

                markets = marketStats
                    .Where(x => x.Type.IsEnabled())
                    .OrderBy(x => x.Type)
                    .ToArray();
            }

            var webProxies = (IEnumerable<SystemStatusWebProxyDTO>)null;
            if (request.IncludeWebProxyStatus)
            {
                webProxies = _mapper.Map<IEnumerable<WebProxyWithUsageStatistics>, IEnumerable<SystemStatusWebProxyDTO>>(
                    (await _webProxyStatisticsService.GetAsync())
                        .OrderByDescending(x => x.LastAccessedOn)
                        .ToArray()
                );
            }

            // TODO: Make these configurable
            var alerts = new List<SystemStatusAlertDTO>();
            if (request.IncludeAlerts)
            {
                /*
                alerts.Add(
                    new SystemStatusAlertDTO()
                    {
                        Severity = SystemStatusAlertSeverity.Warning,
                        Message = "Steam logins are temporarily disabled whilst we investigate the impact of the recently circulating Steam OpenID exploit. There is no breach or leak of SCMM data and your Steam profiles are **not** at risk by using SCMM. This is just a safety precaution until we have had time to investigate the exploit further. Sorry for the inconvenience.",
                        IsVisible = true
                    }
                );
                */
            }

            return new GetSystemStatusResponse()
            {
                Status = new SystemStatusDTO()
                {
                    SteamApp = steamApp,
                    Markets = markets?.ToArray(),
                    WebProxies = webProxies?.ToArray(),
                    Alerts = alerts?.ToArray(),
                }
            };
        }
    }
}
