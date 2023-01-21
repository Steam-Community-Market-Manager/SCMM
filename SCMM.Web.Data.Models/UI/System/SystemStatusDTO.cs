namespace SCMM.Web.Data.Models.UI.System;

public class SystemStatusDTO
{
    public SystemStatusSeverity Status
    {
        get
        {
            if (
                Alerts?.Any(x => x.Severity >= SystemStatusAlertSeverity.Error) == true
            )
            {
                return SystemStatusSeverity.Critical;
            }
            else if (
                Alerts?.Any(x => x.Severity >= SystemStatusAlertSeverity.Warning) == true ||
                SteamApp?.AssetDescriptionsUpdates?.IsOnTarget == false ||
                SteamApp?.MarketOrderUpdates?.IsOnTarget == false ||
                SteamApp?.MarketSaleUpdates?.IsOnTarget == false ||
                WebProxies?.Any(x => !x.IsAvailable) == true
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

    public IEnumerable<SystemStatusAlertDTO> Alerts { get; set; }

    public SystemStatusSteamAppDTO SteamApp { get; set; }

    public IEnumerable<SystemStatusWebProxyDTO> WebProxies { get; set; }
}
