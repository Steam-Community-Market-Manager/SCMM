using System.Text.Json.Serialization;

namespace SCMM.Shared.Abstractions.Messaging;

public abstract class Message : IMessage
{
    [JsonIgnore]
    public virtual string Id => null;
}
