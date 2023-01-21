using AutoMapper;
using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using SCMM.Shared.Abstractions.WebProxies;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Web.Data.Models.UI.System;

namespace SCMM.Web.Server.Queries
{
    public class GetSystemStatusRequest : IQuery<GetSystemStatusResponse>
    {
        public ulong AppId { get; set; }
    }

    public class GetSystemStatusResponse
    {
        public SystemStatusDTO Status { get; set; }
    }

    public class GetSystemStatus : IQueryHandler<GetSystemStatusRequest, GetSystemStatusResponse>
    {
        private readonly SteamDbContext _db;
        private readonly IDistributedCache _cache;
        private readonly IWebProxyManagementService _webProxies;
        private readonly IMapper _mapper;

        public GetSystemStatus(SteamDbContext db, IDistributedCache cache, IWebProxyManagementService webProxies, IMapper mapper)
        {
            _db = db;
            _cache = cache;
            _webProxies = webProxies;
            _mapper = mapper;
        }

        public async Task<GetSystemStatusResponse> HandleAsync(GetSystemStatusRequest request)
        {
            // TODO: Make these configurable
            var alerts = new List<SystemStatusAlertDTO>();
            /*
            alerts.Add(
                new SystemStatusAlertDTO()
                {
                    Severity = SystemStatusAlertSeverity.Warning,
                    Message = "Steam is heavily rate limiting our servers. Market data is taking longer to update than normal. Please be patient as we work through a solution. Sorry for the inconvenience.",
                    IsVisible = true
                }
            );
            */

            var steamApp = await _db.SteamApps
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
                            Size = a.ItemDefinitions.Length,
                            PublishedOn = a.TimePublished,
                            IsImported = true
                        }),
                    AssetDescriptionsUpdates = new TimeRangeWithTargetDTO()
                    {
                        Oldest = x.AssetDescriptions.Where(x => x.ClassId != null).Min(y => y.TimeRefreshed),
                        Newest = x.AssetDescriptions.Where(x => x.ClassId != null).Max(y => y.TimeRefreshed)
                    },
                    MarketOrderUpdates = new TimeRangeWithTargetDTO()
                    {
                        Oldest = x.MarketItems.Min(y => y.LastCheckedOrdersOn),
                        Newest = x.MarketItems.Max(y => y.LastCheckedOrdersOn)
                    },
                    MarketSaleUpdates = new TimeRangeWithTargetDTO()
                    {
                        Oldest = x.MarketItems.Min(y => y.LastCheckedSalesOn),
                        Newest = x.MarketItems.Max(y => y.LastCheckedSalesOn)
                    }
                })
                .FirstOrDefaultAsync();

            if (steamApp != null)
            {
                // TODO: Make these configurable
                steamApp.AssetDescriptionsUpdates.TargetDelta = TimeSpan.FromHours(24);
                steamApp.MarketOrderUpdates.TargetDelta = TimeSpan.FromHours(1);
                steamApp.MarketSaleUpdates.TargetDelta = TimeSpan.FromHours(1);
            }

            // TODO: Cache these
            var webProxies = _mapper.Map<IEnumerable<IWebProxyDetails>, IEnumerable<SystemStatusWebProxyDTO>>(
                await _webProxies.ListWebProxiesAsync()
            );

            return new GetSystemStatusResponse()
            {
                Status = new SystemStatusDTO()
                {
                    Alerts = alerts,
                    SteamApp = steamApp,
                    WebProxies = webProxies,
                }
            };
        }
    }
}
