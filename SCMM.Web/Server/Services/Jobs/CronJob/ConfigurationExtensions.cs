using Microsoft.Extensions.Configuration;
using System.Linq;

namespace SCMM.Web.Server.Services.Jobs.CronJob
{
    public static class ConfigurationExtensions
    {
        public static CronJobConfiguration GetJobConfiguration<T>(this IConfiguration configuration)
        {
            return configuration
                .GetSection("Jobs")
                .GetChildren()
                .Where(x => x.Key == typeof(T).Name)
                .Select(x => x.Get<CronJobConfiguration>())
                .FirstOrDefault();
        }
    }
}
