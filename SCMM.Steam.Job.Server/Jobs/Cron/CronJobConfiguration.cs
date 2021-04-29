namespace SCMM.Steam.Job.Server.Jobs.Cron
{
    public class CronJobConfiguration
    {
        public bool StartImmediately { get; set; }
        public string CronExpression { get; set; }
    }
}
