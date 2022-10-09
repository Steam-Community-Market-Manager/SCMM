namespace SCMM.Shared.Abstractions.Messaging;

public interface IServiceBus
{
    Task ScheduleMessageFromNowAsync<T>(TimeSpan scheduledEnqueueTimeFromNow, T message, CancellationToken cancellationToken = default)
        where T : class, IMessage;

    Task ScheduleMessageAsync<T>(DateTimeOffset scheduledEnqueueTime, T message, CancellationToken cancellationToken = default)
        where T : class, IMessage;

    Task SendMessageAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class, IMessage;

    Task SendMessagesAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default)
        where T : class, IMessage;

    Task<TResponse> SendMessageAndAwaitReplyAsync<TRequest, TResponse>(TRequest message, int maxTimeToWaitSeconds = 30, CancellationToken cancellationToken = default)
        where TRequest : class, IMessage
        where TResponse : class, IMessage;
}
