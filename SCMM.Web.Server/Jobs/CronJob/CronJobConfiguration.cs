namespace SCMM.Web.Server.Jobs.CronJob
{
    public class CronJobConfiguration
    {
        public bool StartImmediately { get; set; }
        public string CronExpression { get; set; }
    }
}
