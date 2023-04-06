using SCMM.Shared.Data.Models.Json.Serialization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SCMM.Shared.Data.Models.Json;

public static class JsonSerializerOptionsExtensions
{
    /// <summary>
    /// System.Text.Json doesn't expose the default options, so we have to do it via reflection (yuk!)
    /// https://github.com/dotnet/runtime/issues/31094
    /// </summary>
    public static void SetDefaultOptions()
    {
        var defaultOptions = UseDefaults(new JsonSerializerOptions()
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        });

        typeof(JsonSerializerOptions)
            ?.GetField("s_defaultOptions", BindingFlags.Static | BindingFlags.NonPublic)
            ?.SetValue(null, defaultOptions);
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
        options.IgnoreReadOnlyProperties = false; // explicitly use [JsonIgnore] instead
        options.IgnoreReadOnlyFields = true; // use properties only
        options.IncludeFields = false; // use properties only
        options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        options.PropertyNameCaseInsensitive = true; // because javascript
        options.ReadCommentHandling = JsonCommentHandling.Skip;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
#if DEBUG
        // TODO: This causes a JSON parsing error in Swagger UI, reenable after they fix it
        //options.WriteIndented = true;
#endif
        return options;
    }
}
