using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Community.Responses.Xml;
using System.Net;
using System.Xml.Linq;

namespace SCMM.Steam.Client
{
    /// <summary>
    /// Client for https://steamcommunity.com/
    /// Some requests require a valid Steam session cookie
    /// </summary>
    public class SteamCommunityWebClient : SteamWebClient
    {
        public SteamCommunityWebClient(ILogger<SteamCommunityWebClient> logger, IDistributedCache cache, SteamSession session, IWebProxy proxy)
            : base(logger, cache, session: session, proxy: proxy)
        {
            // Transport
            DefaultHeaders.Add("Host", new Uri(Constants.SteamCommunityUrl).Host);
            DefaultHeaders.Add("Referer", Constants.SteamCommunityUrl + "/");
            DefaultHeaders.Add("Connection", "keep-alive");
            
            // Security
            DefaultHeaders.Add("Sec-Fetch-Dest", "empty");
            DefaultHeaders.Add("Sec-Fetch-Mode", "cors");
            DefaultHeaders.Add("Sec-Fetch-Site", "same-origin");

            // Client
            DefaultHeaders.Add("Accept", "*/*");
            DefaultHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            DefaultHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            DefaultHeaders.Add("User-Agent", "Mozilla/5.0 (Windows; U; Windows NT 10.0; en-US; Valve Steam Client/default/1666144119; ) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.121 Safari/537.36");
            DefaultHeaders.Add("X-Requested-With", "XMLHttpRequest");

        }

        #region Profile

        public async Task<SteamProfileXmlResponse> GetProfileById(SteamProfileByIdPageRequest request, bool useCache = true)
        {
            return await GetXml<SteamProfileByIdPageRequest, SteamProfileXmlResponse>(request, useCache);
        }

        #endregion

        #region Inventory

        public async Task<SteamInventoryPaginatedJsonResponse> GetInventoryPaginated(SteamInventoryPaginatedJsonRequest request, bool useCache = true)
        {
            return await GetJson<SteamInventoryPaginatedJsonRequest, SteamInventoryPaginatedJsonResponse>(request, useCache);
        }

        #endregion

        #region Item Store

        public async Task<XElement> GetStorePage(SteamItemStorePageRequest request, bool useCache = false)
        {
            return await GetHtml<SteamItemStorePageRequest>(request, useCache);
        }

        public async Task<SteamItemStoreGetItemDefsPaginatedJsonResponse> GetStorePaginated(SteamItemStoreGetItemDefsPaginatedJsonRequest request, bool useCache = false)
        {
            return await GetJson<SteamItemStoreGetItemDefsPaginatedJsonRequest, SteamItemStoreGetItemDefsPaginatedJsonResponse>(request, useCache);
        }

        #endregion

        #region Market

        public async Task<SteamMarketMyListingsPaginatedJsonResponse> GetMarketMyListingsPaginated(SteamMarketMyListingsPaginatedJsonRequest request, bool useCache = false)
        {
            return await GetJson<SteamMarketMyListingsPaginatedJsonRequest, SteamMarketMyListingsPaginatedJsonResponse>(request, useCache);
        }

        public async Task<SteamMarketMyHistoryPaginatedJsonResponse> GetMarketMyHistoryPaginated(SteamMarketMyHistoryPaginatedJsonRequest request, bool useCache = false)
        {
            return await GetJson<SteamMarketMyHistoryPaginatedJsonRequest, SteamMarketMyHistoryPaginatedJsonResponse>(request, useCache);
        }

        public async Task<SteamMarketSearchPaginatedJsonResponse> GetMarketSearchPaginated(SteamMarketSearchPaginatedJsonRequest request, bool useCache = false)
        {
            return await GetJson<SteamMarketSearchPaginatedJsonRequest, SteamMarketSearchPaginatedJsonResponse>(request, useCache);
        }

        public async Task<SteamMarketItemOrdersActivityJsonResponse> GetMarketItemOrdersActivity(SteamMarketItemOrdersActivityJsonRequest request, bool useCache = false)
        {
            return await GetJson<SteamMarketItemOrdersActivityJsonRequest, SteamMarketItemOrdersActivityJsonResponse>(request, useCache);
        }

        public async Task<SteamMarketItemOrdersHistogramJsonResponse> GetMarketItemOrdersHistogram(SteamMarketItemOrdersHistogramJsonRequest request, bool useCache = false)
        {
            return await GetJson<SteamMarketItemOrdersHistogramJsonRequest, SteamMarketItemOrdersHistogramJsonResponse>(request, useCache);
        }

        public async Task<SteamMarketPriceOverviewJsonResponse> GetMarketPriceOverview(SteamMarketPriceOverviewJsonRequest request, bool useCache = false)
        {
            return await GetJson<SteamMarketPriceOverviewJsonRequest, SteamMarketPriceOverviewJsonResponse>(request, useCache);
        }

        public async Task<SteamMarketPriceHistoryJsonResponse> GetMarketPriceHistory(SteamMarketPriceHistoryJsonRequest request, bool useCache = false)
        {
            return await GetJson<SteamMarketPriceHistoryJsonRequest, SteamMarketPriceHistoryJsonResponse>(request, useCache);
        }

        #endregion
    }
}
