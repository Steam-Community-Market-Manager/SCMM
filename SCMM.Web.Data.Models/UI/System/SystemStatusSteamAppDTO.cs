using SCMM.Steam.Data.Models.Enums;

namespace SCMM.Web.Data.Models.UI.System;

public class SystemStatusSteamAppDTO
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string IconUrl { get; set; }

    public IEnumerable<SystemStatusAppItemDefinitionArchive> ItemDefinitionArchives { get; set; }

    public TimeRangeWithTargetDTO AssetDescriptionsUpdates { get; set; }

    public TimeRangeWithTargetDTO MarketOrderUpdates { get; set; }

    public TimeRangeWithTargetDTO MarketSaleUpdates { get; set; }

    public IDictionary<MarketType, SystemStatusAppMarketDTO> Markets { get; set; }
}
