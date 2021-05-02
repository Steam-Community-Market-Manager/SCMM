using System.Threading.Tasks;

namespace SCMM.Shared.Azure.ServiceBus
{
    public interface IMessageHandler<T> : IMessageHandler where T : IMessage
    {
        Task HandleAsync(T message);
    }

    public interface IMessageHandler
    {
    }
}
