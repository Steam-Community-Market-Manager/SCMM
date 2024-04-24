namespace SCMM.Web.Server;

public static class CachePolicy
{
    public const string Expire3m = "Expire3m";
    public const string Expire10m = "Expire10m";
    public const string Expire30m = "Expire30m";
    public const string Expire1h = "Expire1h";
    public const string Expire1d = "Expire1d";
    public const string Expire7d = "Expire7d";
}

public static class CacheTag
{
    public const string All = "all";
    public const string App = "app";
    public const string Currency = "currency";
    public const string Language = "language";
    public const string System = "system";
    public const string Statistics = "statistics";
    public const string ItemDefinition = "itemDefinition";
    public const string Item = "item";
    public const string Store = "store";
    public const string Market = "market";
}
