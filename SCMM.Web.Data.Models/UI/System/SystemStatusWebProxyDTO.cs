namespace SCMM.Web.Data.Models.UI.System;

public class SystemStatusWebProxyDTO
{
    public string Id { get; set; }

    public string Address { get; set; }

    public string CountryFlag { get; set; }

    public string CountryCode { get; set; }

    public string CityName { get; set; }

    public bool IsAvailable { get; set; }

    public DateTimeOffset LastCheckedOn { get; set; }

    public IDictionary<string, DateTimeOffset> DomainRateLimits { get; set; }
}
