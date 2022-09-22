using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Community.Responses.Xml;
using System.Xml.Linq;

namespace SCMM.Steam.Client
{
    /// <summary>
    /// Client for https://steamcommunity.com/
    /// Some requests require a valid Steam session cookie
    /// </summary>
    public class SteamCommunityWebClient : SteamWebClient
    {
        public SteamCommunityWebClient(ILogger<SteamCommunityWebClient> logger, IDistributedCache cache, SteamSession session)
            : base(logger, cache, session)
        {
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

        public async Task<XElement> GetStoreItemPage(SteamItemStoreDetailPageRequest request, bool useCache = false)
        {
            return await GetHtml<SteamItemStoreDetailPageRequest>(request, useCache);
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
