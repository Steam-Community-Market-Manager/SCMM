namespace SCMM.Shared.Abstractions.Messaging;

public interface IMessageContext
{
    string MessageId { get; }

    Type MessageType { get; }

    string ReplyTo { get; }

    Task ReplyAsync<T>(T message, CancellationToken cancellationToken = default) where T : class, IMessage;
}
