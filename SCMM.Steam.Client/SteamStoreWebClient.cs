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
    /// Client for https://steamcommunity.com/.
    /// Most requests can be done anonymous, some require a valid Steam session cookie.
    /// </summary>
    public class SteamStoreWebClient : SteamWebClient
    {
        public SteamStoreWebClient(ILogger<SteamStoreWebClient> logger, IDistributedCache cache)
            : base(logger, cache)
        {
        }
    }

    /// <inheritdoc />
    public class ProxiedSteamStoreWebClient : SteamWebClient
    {
        public ProxiedSteamStoreWebClient(ILogger<SteamStoreWebClient> logger, IDistributedCache cache, IWebProxy proxy)
            : base(logger, cache, proxy: proxy)
        {
            // Transport
            DefaultHeaders.Add("Host", new Uri(Constants.SteamStoreUrl).Host);
            DefaultHeaders.Add("Referer", Constants.SteamStoreUrl + "/");
            DefaultHeaders.Add("Connection", "keep-alive");

            // Security
            DefaultHeaders.Add("sec-ch-ua", @"""Chromium"";v=""106"", ""Google Chrome"";v=""106"", ""Not;A=Brand"";v=""99""");
            DefaultHeaders.Add("sec-ch-ua-mobile", "?1");
            DefaultHeaders.Add("sec-ch-ua-platform", @"""Android""");
            DefaultHeaders.Add("Sec-Fetch-Dest", "empty");
            DefaultHeaders.Add("Sec-Fetch-Mode", "cors");
            DefaultHeaders.Add("Sec-Fetch-Site", "same-origin");

            // Client
            DefaultHeaders.Add("Accept", "*/*");
            DefaultHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            DefaultHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            DefaultHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/106.0.0.0 Mobile Safari/537.36");
            DefaultHeaders.Add("X-Requested-With", "XMLHttpRequest");
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
