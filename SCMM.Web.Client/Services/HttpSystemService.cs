using SCMM.Shared.Data.Models.Json;
using SCMM.Web.Data.Models.Services;
using SCMM.Web.Data.Models.UI.System;

namespace SCMM.Web.Client.Services;

public class HttpSystemService : ISystemService
{
    private readonly HttpClient _http;

    public HttpSystemService(HttpClient http)
    {
        _http = http;
    }

    public Task<SystemStatusDTO> GetSystemStatusAsync(ulong appId, bool includeWebProxiesStatus = false)
    {
        return _http.GetFromJsonWithDefaultsAsync<SystemStatusDTO>($"api/system/status?appId={appId}&includeWebProxiesStatus={includeWebProxiesStatus}");
    }

    public Task<IEnumerable<SystemUpdateMessageDTO>> ListLatestSystemUpdateMessagesAsync()
    {
        return _http.GetFromJsonWithDefaultsAsync<IEnumerable<SystemUpdateMessageDTO>>($"api/system/latestUpdates");
    }
}
