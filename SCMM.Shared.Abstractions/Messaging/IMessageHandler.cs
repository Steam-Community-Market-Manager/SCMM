namespace SCMM.Shared.Abstractions.Messaging;

public interface IMessageHandler<T> : IMessageHandler where T : IMessage
{
    Task HandleAsync(T message, IMessageContext context);
}

public interface IMessageHandler
{
}
