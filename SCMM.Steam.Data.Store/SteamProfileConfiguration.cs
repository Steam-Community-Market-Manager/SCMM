﻿using SCMM.Data.Shared.Store;
using System.Collections.Generic;

namespace SCMM.Steam.Data.Store
{
    public class SteamProfileConfiguration : Configuration
    {
        public static IEnumerable<ConfigurationDefinition> Definitions => new ConfigurationDefinition[]
        {
        };
    }
}