using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Store.Requests.Html;
using SCMM.Steam.Data.Models.Store.Requests.Json;
using SCMM.Steam.Data.Models.Store.Responses.Json;
using System.Net;
using System.Xml.Linq;

namespace SCMM.Steam.Client
{
    /// <summary>
    /// Client for https://store.steampowered.com
    /// </summary>
    public class SteamStoreWebClient : SteamWebClientBase
    {
        public SteamStoreWebClient(ILogger<SteamStoreWebClient> logger, IDistributedCache cache, IWebProxy proxy)
            : base(logger, cache, proxy: proxy)
        {
            DefaultHeaders.Add("Host", new Uri(Constants.SteamStoreUrl).Host);
            DefaultHeaders.Add("Referer", Constants.SteamStoreUrl + "/");
        }

        #region Item Store

        public async Task<XElement> GetStorePage(SteamItemStorePageRequest request, bool useCache = false)
        {
            return await GetHtml<SteamItemStorePageRequest>(request, useCache);
        }

        public async Task<XElement> GetStoreDetailPage(SteamItemStoreDetailPageRequest request, bool useCache = false)
        {
            return await GetHtml<SteamItemStoreDetailPageRequest>(request, useCache);
        }

        public async Task<SteamItemStoreGetItemDefsPaginatedJsonResponse> GetStorePaginated(SteamItemStoreGetItemDefsPaginatedJsonRequest request, bool useCache = false)
        {
            return await GetJson<SteamItemStoreGetItemDefsPaginatedJsonRequest, SteamItemStoreGetItemDefsPaginatedJsonResponse>(request, useCache);
        }

        #endregion
    }
}
