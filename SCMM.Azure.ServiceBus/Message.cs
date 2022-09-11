namespace SCMM.Azure.ServiceBus
{
    public abstract class Message : IMessage
    {
        public virtual string Id => null;
    }
}
