﻿using Microsoft.Extensions.Configuration;
using System.Linq;
using SCMM.Web.Server.Jobs.CronJob;

namespace SCMM.Web.Server.Jobs.CronJob
{
    public static class ConfigurationExtensions
    {
        public static CronJobConfiguration GetJobConfiguration<T>(this IConfiguration configuration)
        {
            return configuration.GetJobConfiguration<T, CronJobConfiguration>();
        }

        public static TC GetJobConfiguration<T, TC>(this IConfiguration configuration)
            where TC : CronJobConfiguration
        {
            return configuration
                .GetSection("Jobs")
                .GetChildren()
                .Where(x => x.Key == typeof(T).Name)
                .Select(x => x.Get<TC>())
                .FirstOrDefault();
        }
    }
}
