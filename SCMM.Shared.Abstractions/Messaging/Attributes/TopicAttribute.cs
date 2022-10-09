namespace SCMM.Shared.Abstractions.Messaging.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class TopicAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
