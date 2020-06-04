using System;

namespace SCMM.Web.Server.Configuration
{
    public class JobConfiguration
    {
        public bool StartImmediately { get; set; }
        public string CronExpression { get; set; }
    }
}
