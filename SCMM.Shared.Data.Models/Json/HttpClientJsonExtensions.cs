using System.Net.Http.Json;
using System.Text.Json;

namespace SCMM.Shared.Data.Models.Json;

public static class HttpClientJsonExtensions
{
    public static Task<TValue> GetFromJsonWithDefaultsAsync<TValue>(this HttpClient client, string requestUri, CancellationToken cancellationToken = default)
    {
        return client.GetFromJsonAsync<TValue>(requestUri, new JsonSerializerOptions().UseDefaults(), cancellationToken);
    }

    public static Task<HttpResponseMessage> PostAsJsonWithDefaultsAsync<TValue>(this HttpClient client, string requestUri, TValue value, CancellationToken cancellationToken = default)
    {
        return client.PostAsJsonAsync<TValue>(requestUri, value, new JsonSerializerOptions().UseDefaults(), cancellationToken);
    }

    public static Task<HttpResponseMessage> PutAsJsonWithDefaultsAsync<TValue>(this HttpClient client, string requestUri, TValue value, CancellationToken cancellationToken = default)
    {
        return client.PutAsJsonAsync<TValue>(requestUri, value, new JsonSerializerOptions().UseDefaults(), cancellationToken);
    }
}