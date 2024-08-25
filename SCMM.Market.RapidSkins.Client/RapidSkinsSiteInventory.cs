namespace SCMM.Market.RapidSkins.Client;

public class RapidSkinsSiteInventory : Dictionary<string, Dictionary<string, RapidSkinsPaginatedItems>>
{
    public IDictionary<string, RapidSkinsPaginatedItems> SiteInventory => this.GetValueOrDefault("siteInventory");
}
