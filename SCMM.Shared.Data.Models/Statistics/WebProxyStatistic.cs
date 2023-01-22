
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

        public DateTimeOffset? LastAccessedOn { get; set; }

        public int RequestsSucceededCount { get; set; }

        public int RequestsFailedCount { get; set; }

        public IDictionary<string, DateTimeOffset> DomainRateLimits { get; set; }
    }
}
