using Microsoft.Extensions.Configuration;
using System.Linq;

namespace SCMM.Web.Server.Configuration
{
    public static class ConfigurationExtensions
    {
        public static JobConfiguration GetJobConfiguration<T>(this IConfiguration configuration)
        {
            return configuration
                .GetSection("Jobs")
                .GetChildren()
                .Where(x => x.Key == typeof(T).Name)
                .Select(x => x.Get<JobConfiguration>())
                .FirstOrDefault();
        }
    }
}
