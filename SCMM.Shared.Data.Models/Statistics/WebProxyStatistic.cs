using System.Text.Json.Serialization;

namespace SCMM.Shared.Data.Models.Statistics
{
    public class WebProxyStatistic
    {
        public string Source { get; set; }

        public string Id { get; set; }

        public string Address { get; set; }

        public int Port { get; set; }

        public string CountryCode { get; set; }

        public string CityName { get; set; }

        public bool IsAvailable { get; set; }

        public DateTimeOffset? LastCheckedOn { get; set; }

        public DateTimeOffset? LastUsedOn { get; set; }

        public int RequestSuccessCount { get; set; }

        public int RequestFailCount { get; set; }

        public IDictionary<string, DateTimeOffset> DomainRateLimits { get; set; }
    }
}
