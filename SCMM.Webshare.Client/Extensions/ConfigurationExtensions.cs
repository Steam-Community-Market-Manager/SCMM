﻿using Microsoft.Extensions.Configuration;

namespace SCMM.Webshare.Client.Extensions
{
    public static class ConfigurationExtensions
    {
        public static WebshareConfiguration GetWebshareConfiguration(this IConfiguration configuration)
        {
            return configuration
                .GetSection("Webshare")
                .Get<WebshareConfiguration>();
        }
    }
}
