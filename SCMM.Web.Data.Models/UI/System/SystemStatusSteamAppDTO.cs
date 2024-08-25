namespace SCMM.Web.Data.Models.UI.System;

public class SystemStatusSteamAppDTO
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string IconUrl { get; set; }

    public SystemStatusAppItemDefinitionArchive[] ItemDefinitionArchives { get; set; }

    public TimeRangeWithTargetDTO AssetDescriptionsUpdates { get; set; }

    public TimeRangeWithTargetDTO MarketOrderUpdates { get; set; }

    public TimeRangeWithTargetDTO MarketSaleUpdates { get; set; }

    public TimeRangeWithTargetDTO MarketActivityUpdates { get; set; }
}
