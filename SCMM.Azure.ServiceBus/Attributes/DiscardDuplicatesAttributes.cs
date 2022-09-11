namespace SCMM.Azure.ServiceBus.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DuplicateDetectionAttribute : Attribute
    {
        public double DiscardDuplicatesSentWithinLastMinutes { get; set; } = TimeSpan.FromDays(1).TotalSeconds;
    }
}
