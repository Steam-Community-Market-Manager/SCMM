using System;

namespace SCMM.Web.Data.Models.UI.System;

public class AppStatusDTO
{
    public string SteamId { get; set; }

    public string Name { get; set; }

    public string IconUrl { get; set; }

    public string ItemDefinitionsDigest { get; set; }

    public DateTimeOffset? ItemDefinitionsLastModified { get; set; }

    public Tuple<DateTimeOffset?, DateTimeOffset?> LastCheckedAssetDescriptions { get; set; }

    public Tuple<DateTimeOffset?, DateTimeOffset?> LastCheckedMarketOrders { get; set; }

    public Tuple<DateTimeOffset?, DateTimeOffset?> LastCheckedMarketSales { get; set; }
}
