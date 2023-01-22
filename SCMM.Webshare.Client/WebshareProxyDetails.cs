using SCMM.Shared.Abstractions.WebProxies;
using System.Text.Json.Serialization;

namespace SCMM.Webshare.Client;

public class WebshareProxyDetails : IWebProxyDetails
{
    public string Source => "proxy.webshare.io";

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("proxy_address")]
    public string Address { get; set; }

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; }

    [JsonPropertyName("city_name")]
    public string CityName { get; set; }

    [JsonPropertyName("valid")]
    public bool IsAvailable { get; set; }

    [JsonPropertyName("last_verification")]
    public DateTimeOffset LastCheckedOn { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedOn { get; set; }
}
