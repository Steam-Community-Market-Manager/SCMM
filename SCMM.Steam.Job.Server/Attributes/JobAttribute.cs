namespace SCMM.Steam.Job.Server.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class JobAttribute : Attribute
{
    public JobAttribute(string name, int everySeconds)
    {
        Name = name;
        EverySeconds = everySeconds;
    }

    public JobAttribute(string name, string cronSchedule)
    {
        Name = name;
        CronSchedule = cronSchedule;
    }

    public string Name { get; set; }

    public int? EverySeconds { get; set; }

    public string CronSchedule { get; set; }

}
