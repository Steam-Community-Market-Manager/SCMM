using System.Text.Json;

namespace SCMM.Azure.ServiceBus.Extensions;

public static class ObjectExtensions
{
    public static ReadOnlyMemory<byte> ToBinaryAsJson(this object body)
    {
        return new BinaryData(
            JsonSerializer.SerializeToUtf8Bytes(body, body.GetType())
        );
    }
}
