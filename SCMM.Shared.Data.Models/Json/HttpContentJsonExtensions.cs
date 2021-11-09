using System.Net.Http.Json;
using System.Text.Json;

namespace SCMM.Shared.Data.Models.Json;

public static class HttpContentJsonExtensions
{
    public static Task<T> ReadFromJsonWithDefaultsAsync<T>(this HttpContent content, CancellationToken cancellationToken = default)
    {
        return content.ReadFromJsonAsync<T>(new JsonSerializerOptions().UseDefaults(), cancellationToken);
    }
}