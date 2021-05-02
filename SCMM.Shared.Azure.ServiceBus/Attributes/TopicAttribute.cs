using System;

namespace SCMM.Shared.Azure.ServiceBus.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TopicAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
