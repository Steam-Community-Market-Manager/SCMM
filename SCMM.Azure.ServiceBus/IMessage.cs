namespace SCMM.Azure.ServiceBus
{
    public interface IMessage
    {
        public const string ApplicationPropertyType = "Type";

        public string Id { get; }
    }
}
