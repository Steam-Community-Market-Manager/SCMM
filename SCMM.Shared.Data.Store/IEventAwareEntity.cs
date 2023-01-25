using SCMM.Shared.Abstractions.Messaging;

namespace SCMM.Shared.Data.Store;

public interface IEventAwareEntity
{
    public IEnumerable<IMessage> RaisedEvents { get; }

    void RaiseEvent(IMessage message);

    void ClearEvents();
}
