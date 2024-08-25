using SCMM.Web.Data.Models.UI.System;

namespace SCMM.Web.Data.Models.Services;
public interface ISystemService
{
    Task<SystemStatusDTO> GetSystemStatusAsync(ulong appId, bool includeAppStatus = false, bool includeMarketStatus = false, bool includeWebProxyStatus = false);

    Task<IEnumerable<SystemUpdateMessageDTO>> ListLatestSystemUpdateMessagesAsync();
}
