using Microsoft.Extensions.Logging;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Blob;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Community.Responses.Xml;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SCMM.Steam.Client
{
    public class SteamCommunityWebClient : SteamWebClient
    {
        private readonly ILogger<SteamCommunityWebClient> _logger;

        public SteamCommunityWebClient(ILogger<SteamCommunityWebClient> logger, SteamSession session)
            : base(logger, session)
        {
            _logger = logger;
        }

        public async Task<SteamProfileXmlResponse> GetProfile(SteamProfilePageRequest request)
        {
            return await GetXml<SteamProfilePageRequest, SteamProfileXmlResponse>(request);
        }

        public async Task<SteamMarketAppFiltersJsonResponse> GetMarketAppFilters(SteamMarketAppFiltersJsonRequest request)
        {
            return await GetJson<SteamMarketAppFiltersJsonRequest, SteamMarketAppFiltersJsonResponse>(request);
        }

        public async Task<XElement> GetItemWorkshopPage(SteamWorkshopBrowsePageRequest request)
        {
            return await GetHtml<SteamWorkshopBrowsePageRequest>(request);
        }

        public async Task<XElement> GetItemStorePage(SteamItemStorePageRequest request)
        {
            return await GetHtml<SteamItemStorePageRequest>(request);
        }

        public async Task<SteamItemStorePaginatedJsonResponse> GetItemStorePaginated(SteamItemStorePaginatedJsonRequest request)
        {
            return await GetJson<SteamItemStorePaginatedJsonRequest, SteamItemStorePaginatedJsonResponse>(request);
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

        public async Task<string> GetMarketListingItemNameId(SteamMarketListingPageRequest request)
        {
            var html = await GetText(request);
            if (String.IsNullOrEmpty(html))
            {
                return null;
            }

            // TODO: Find a better way to look this up...
            var itemNameIdMatchGroup = Regex.Match(html, Constants.SteamMarketListingItemNameIdRegex).Groups;
            return (itemNameIdMatchGroup.Count > 1)
                ? itemNameIdMatchGroup[1].Value.Trim()
                : null;
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
            // API returns BadRequest unless authenticated
            if (Session?.IsLoggedIn != true)
            {
                _logger.LogError($"GET '{request}' was skipped because session is not authenticated");
                return null;
            }

            return await GetJson<SteamMarketPriceHistoryJsonRequest, SteamMarketPriceHistoryJsonResponse>(request);
        }
    }
}
