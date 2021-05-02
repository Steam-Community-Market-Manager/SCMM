using System;

namespace SCMM.Shared.Azure.ServiceBus.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ConcurrencyAttribute : Attribute
    {
        public int MaxConcurrentCalls { get; set; } = 1;
    }
}
