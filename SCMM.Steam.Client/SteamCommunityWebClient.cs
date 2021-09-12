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

        public async Task<SteamProfileXmlResponse> GetCustomProfile(SteamCustomProfilePageRequest request)
        {
            return await GetXml<SteamCustomProfilePageRequest, SteamProfileXmlResponse>(request);
        }

        public async Task<XElement> GetStorePage(SteamStorePageRequest request)
        {
            return await GetHtml<SteamStorePageRequest>(request);
        }

        public async Task<SteamStorePaginatedJsonResponse> GetStorePaginated(SteamStorePaginatedJsonRequest request)
        {
            return await GetJson<SteamStorePaginatedJsonRequest, SteamStorePaginatedJsonResponse>(request);
        }

        public async Task<XElement> GetStoreItemPage(SteamStoreItemPageRequest request)
        {
            return await GetHtml<SteamStoreItemPageRequest>(request);
        }

        public async Task<SteamInventoryPaginatedJsonResponse> GetInventoryPaginated(SteamInventoryPaginatedJsonRequest request)
        {
            return await GetJson<SteamInventoryPaginatedJsonRequest, SteamInventoryPaginatedJsonResponse>(request);
        }

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
    }
}
