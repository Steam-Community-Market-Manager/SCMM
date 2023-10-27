using System.Text.Json.Serialization;

namespace SCMM.Shared.Abstractions.WebProxies
{
    public class WebProxyWithUsageStatistics : IWebProxyDetails
    {
        public string Source { get; set; }

        public string Id { get; set; }

        [JsonIgnore]
        public string Url => new UriBuilder(Uri.UriSchemeHttp, Address, Port).Uri.ToString();

        public string Address { get; set; }

        public int Port { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string CountryCode { get; set; }

        public string CityName { get; set; }

        public bool IsAvailable { get; set; }

        public DateTimeOffset? LastCheckedOn { get; set; }

        public DateTimeOffset? LastAccessedOn { get; set; }

        public int RequestsSucceededCount { get; set; }

        public int RequestsFailedCount { get; set; }

        public IDictionary<string, DateTimeOffset> DomainRateLimits { get; set; }
    }
}
