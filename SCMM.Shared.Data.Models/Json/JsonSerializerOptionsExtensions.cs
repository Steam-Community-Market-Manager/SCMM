using SCMM.Shared.Data.Models.Json.Serialization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SCMM.Shared.Data.Models.Json;

public static class JsonSerializerOptionsExtensions
{
    /// <summary>
    /// System.Text.Json doesn't expose the default options, so we have to do it via reflection (yuk!)
    /// https://github.com/dotnet/runtime/issues/31094
    /// </summary>
    public static void SetDefaultOptions()
    {
        UseDefaults(
            (JsonSerializerOptions)typeof(JsonSerializerOptions)
                ?.GetField("s_defaultOptions", BindingFlags.Static | BindingFlags.NonPublic)
                ?.GetValue(null)
        );
    }

    public static JsonSerializerOptions UseDefaults(this JsonSerializerOptions options)
    {
        if (options == null)
        {
            return options;
        }

        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new JsonNumberBooleanConverter());
        options.Converters.Add(new JsonNumberStringConverter());
        options.AllowTrailingCommas = true;
        options.IgnoreReadOnlyProperties = true;
        options.IgnoreReadOnlyFields = true;
        options.IncludeFields = false;
        options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        options.PropertyNameCaseInsensitive = true;
        options.ReadCommentHandling = JsonCommentHandling.Skip;
#if DEBUG
        // TODO: This causes a JSON parsing error in Swagger UI, reenable after they fix it
        //options.WriteIndented = true;
#endif
        return options;
    }
}
