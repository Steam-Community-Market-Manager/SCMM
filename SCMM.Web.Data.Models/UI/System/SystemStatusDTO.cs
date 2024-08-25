namespace SCMM.Web.Data.Models.UI.System;

public class SystemStatusDTO
{
    public SystemStatusSteamAppDTO SteamApp { get; set; }

    public SystemStatusAppMarketDTO[] Markets { get; set; }

    public SystemStatusWebProxyDTO[] WebProxies { get; set; }

    public SystemStatusAlertDTO[] Alerts { get; set; }

    public SystemStatusSeverity Status
    {
        get
        {
            if (
                Markets?.Any(x => x.Status >= SystemStatusSeverity.Critical) == true ||
                Alerts?.Any(x => x.Severity >= SystemStatusAlertSeverity.Error) == true
            )
            {
                return SystemStatusSeverity.Critical;
            }
            else if (
                SteamApp?.AssetDescriptionsUpdates?.IsOnTarget == false ||
                SteamApp?.MarketOrderUpdates?.IsOnTarget == false ||
                SteamApp?.MarketSaleUpdates?.IsOnTarget == false ||
                SteamApp?.MarketActivityUpdates?.IsOnTarget == false ||
                Markets?.Any(x => x.Status >= SystemStatusSeverity.Degraded) == true ||
                Alerts?.Any(x => x.Severity >= SystemStatusAlertSeverity.Warning) == true
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
