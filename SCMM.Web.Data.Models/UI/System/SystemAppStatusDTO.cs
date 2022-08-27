using System;

namespace SCMM.Web.Data.Models.UI.System;

public class SystemAppStatusDTO
{
    public string SteamId { get; set; }

    public string Name { get; set; }

    public string IconUrl { get; set; }

    public string ItemDefinitionsDigest { get; set; }

    public DateTimeOffset? ItemDefinitionsLastModified { get; set; }

    public TimeRangeDTO LastCheckedAssetDescriptions { get; set; }

    public TimeRangeDTO LastCheckedMarketOrders { get; set; }

    public TimeRangeDTO LastCheckedMarketSales { get; set; }
}
