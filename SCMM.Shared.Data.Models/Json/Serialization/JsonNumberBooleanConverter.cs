using System.Text.Json;
using System.Text.Json.Serialization;

namespace SCMM.Shared.Data.Models.Json.Serialization;

public class JsonNumberBooleanConverter : JsonConverter<bool>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(bool) == typeToConvert;
    }

    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.TryGetInt64(out var l)
                ? l != 0
                : reader.GetDouble() != 0;
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            return bool.TryParse(reader.GetString(), out var b) ? b : false;
        }
        else if (reader.TokenType == JsonTokenType.True)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}
