using SCMM.Web.Data.Models.UI.System;

namespace SCMM.Web.Data.Models.Services;
public interface ISystemService
{
    Task<SystemStatusDTO> GetSystemStatusAsync(ulong appId, bool includeAppMarkets = false, bool includeWebProxiesStatus = false);

    Task<IEnumerable<SystemUpdateMessageDTO>> ListLatestSystemUpdateMessagesAsync();
}
