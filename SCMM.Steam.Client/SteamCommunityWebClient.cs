using Microsoft.Extensions.Logging;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Community.Responses.Xml;
using System.Xml.Linq;

namespace SCMM.Steam.Client
{
    public class SteamCommunityWebClient : SteamWebClient
    {
        public SteamCommunityWebClient(ILogger<SteamCommunityWebClient> logger, SteamSession session)
            : base(logger, session)
        {
        }

        #region Profile

        public async Task<SteamProfileXmlResponse> GetProfileById(SteamProfileByIdPageRequest request)
        {
            return await GetXml<SteamProfileByIdPageRequest, SteamProfileXmlResponse>(request);
        }

        #endregion

        #region Inventory

        public async Task<SteamInventoryPaginatedJsonResponse> GetInventoryPaginated(SteamInventoryPaginatedJsonRequest request)
        {
            return await GetJson<SteamInventoryPaginatedJsonRequest, SteamInventoryPaginatedJsonResponse>(request);
        }

        #endregion

        #region Item Store

        public async Task<XElement> GetStorePage(SteamItemStorePageRequest request)
        {
            return await GetHtml<SteamItemStorePageRequest>(request);
        }

        public async Task<SteamItemStoreGetItemDefsPaginatedJsonResponse> GetStorePaginated(SteamItemStoreGetItemDefsPaginatedJsonRequest request)
        {
            return await GetJson<SteamItemStoreGetItemDefsPaginatedJsonRequest, SteamItemStoreGetItemDefsPaginatedJsonResponse>(request);
        }

        public async Task<XElement> GetStoreItemPage(SteamItemStoreDetailPageRequest request)
        {
            return await GetHtml<SteamItemStoreDetailPageRequest>(request);
        }

        #endregion

        #region Market

        public async Task<SteamMarketMyListingsPaginatedJsonResponse> GetMarketMyListingsPaginated(SteamMarketMyListingsPaginatedJsonRequest request)
        {
            return await GetJson<SteamMarketMyListingsPaginatedJsonRequest, SteamMarketMyListingsPaginatedJsonResponse>(request);
        }

        public async Task<SteamMarketMyHistoryPaginatedJsonResponse> GetMarketMyHistoryPaginated(SteamMarketMyHistoryPaginatedJsonRequest request)
        {
            return await GetJson<SteamMarketMyHistoryPaginatedJsonRequest, SteamMarketMyHistoryPaginatedJsonResponse>(request);
        }

        public async Task<SteamMarketSearchPaginatedJsonResponse> GetMarketSearchPaginated(SteamMarketSearchPaginatedJsonRequest request)
        {
            return await GetJson<SteamMarketSearchPaginatedJsonRequest, SteamMarketSearchPaginatedJsonResponse>(request);
        }

        public async Task<SteamMarketItemOrdersActivityJsonResponse> GetMarketItemOrdersActivity(SteamMarketItemOrdersActivityJsonRequest request)
        {
            return await GetJson<SteamMarketItemOrdersActivityJsonRequest, SteamMarketItemOrdersActivityJsonResponse>(request);
        }

        public async Task<SteamMarketItemOrdersHistogramJsonResponse> GetMarketItemOrdersHistogram(SteamMarketItemOrdersHistogramJsonRequest request)
        {
            return await GetJson<SteamMarketItemOrdersHistogramJsonRequest, SteamMarketItemOrdersHistogramJsonResponse>(request);
        }

        public async Task<SteamMarketPriceOverviewJsonResponse> GetMarketPriceOverview(SteamMarketPriceOverviewJsonRequest request)
        {
            return await GetJson<SteamMarketPriceOverviewJsonRequest, SteamMarketPriceOverviewJsonResponse>(request);
        }

        public async Task<SteamMarketPriceHistoryJsonResponse> GetMarketPriceHistory(SteamMarketPriceHistoryJsonRequest request)
        {
            return await GetJson<SteamMarketPriceHistoryJsonRequest, SteamMarketPriceHistoryJsonResponse>(request);
        }

        #endregion
    }
}
