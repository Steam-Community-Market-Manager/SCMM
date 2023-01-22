using SCMM.Shared.Data.Models.Extensions;

namespace SCMM.Web.Data.Models.UI.System;

public class SystemStatusDTO
{
    public IEnumerable<SystemStatusAlertDTO> Alerts { get; set; }

    public SystemStatusSteamAppDTO SteamApp { get; set; }

    public IEnumerable<SystemStatusWebProxyDTO> WebProxies { get; set; }

    public SystemStatusSeverity Status
    {
        get
        {
            if (
                Alerts?.Any(x => x.Severity >= SystemStatusAlertSeverity.Error) == true ||
                (WebProxies?.Count(x => x.Status >= SystemStatusSeverity.Degraded) ?? 0).ToPercentage(WebProxies?.Count() ?? 0) >= 0.9m // 90% or more
            )
            {
                return SystemStatusSeverity.Critical;
            }
            else if (
                Alerts?.Any(x => x.Severity >= SystemStatusAlertSeverity.Warning) == true ||
                SteamApp?.AssetDescriptionsUpdates?.IsOnTarget == false ||
                SteamApp?.MarketOrderUpdates?.IsOnTarget == false ||
                SteamApp?.MarketSaleUpdates?.IsOnTarget == false ||
                (WebProxies?.Count(x => x.Status >= SystemStatusSeverity.Degraded) ?? 0).ToPercentage(WebProxies?.Count() ?? 0) >= 0.6m // 60% or more
            )
            {
                return SystemStatusSeverity.Degraded;
            }
            else
            {
                return SystemStatusSeverity.Normal;
            }
        }
    }
}
