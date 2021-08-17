namespace SCMM.Azure.ServiceBus.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class PrefetchAttribute : Attribute
    {
        public int PrefetchCount { get; set; } = 0;
    }
}
