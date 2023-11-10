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
    /// </summary>
    /// <remarks>
    /// TODO: Confirm the exact rate-limit rules for SCM.
    /// You are limited to 25 requests within 30 seconds, which resets after ???.
    /// </remarks>
    public class SteamCommunityWebClient : SteamWebClientBase
    {
        public SteamCommunityWebClient(ILogger<SteamCommunityWebClient> logger, IDistributedCache cache, IWebProxy proxy)
            : base(logger, cache, proxy: proxy)
        {
            DefaultHeaders.Add("Host", new Uri(Constants.SteamCommunityUrl).Host);
            DefaultHeaders.Add("Referer", Constants.SteamCommunityUrl + "/");
        }

        #region Profile

        public async Task<SteamProfileXmlResponse> GetProfileByIdAsync(SteamProfileByIdPageRequest request, bool useCache = true)
        {
            return await GetXmlAsync<SteamProfileByIdPageRequest, SteamProfileXmlResponse>(request, useCache);
        }

        public async Task<XElement> GetProfileMyWorkshopFilesPageAsync(SteamProfileMyWorkshopFilesPageRequest request, bool useCache = false)
        {
            return await GetHtmlAsync<SteamProfileMyWorkshopFilesPageRequest>(request, useCache);
        }

        #endregion

        #region Inventory

        public async Task<SteamInventoryPaginatedJsonResponse> GetInventoryPaginatedAsync(SteamInventoryPaginatedJsonRequest request, bool useCache = true)
        {
            try
            {
                DefaultHeaders["Referer"] = new SteamProfileInventoryPageRequest()
                {
                    AppId = request.AppId,
                    SteamId = request.SteamId,
                };

                return await GetJsonAsync<SteamInventoryPaginatedJsonRequest, SteamInventoryPaginatedJsonResponse>(request, useCache);
            }
            finally
            {
                DefaultHeaders["Referer"] = Constants.SteamCommunityUrl + "/";
            }
        }

        #endregion

        #region Market

        public async Task<SteamMarketAppFiltersJsonResponse> GetMarketAppFiltersAsync(SteamMarketAppFiltersJsonRequest request, bool useCache = false)
        {
            return await GetJsonAsync<SteamMarketAppFiltersJsonRequest, SteamMarketAppFiltersJsonResponse>(request, useCache);
        }

        public async Task<SteamMarketSearchPaginatedJsonResponse> GetMarketSearchPaginatedAsync(SteamMarketSearchPaginatedJsonRequest request, bool useCache = false)
        {
            return await GetJsonAsync<SteamMarketSearchPaginatedJsonRequest, SteamMarketSearchPaginatedJsonResponse>(request, useCache);
        }

        public async Task<SteamMarketPriceOverviewJsonResponse> GetMarketPriceOverviewAsync(SteamMarketPriceOverviewJsonRequest request, bool useCache = false)
        {
            try
            {
                DefaultHeaders["Referer"] = new SteamMarketListingPageRequest()
                {
                    AppId = request.AppId,
                    MarketHashName = request.MarketHashName,
                };

                return await GetJsonAsync<SteamMarketPriceOverviewJsonRequest, SteamMarketPriceOverviewJsonResponse>(request, useCache);
            }
            finally
            {
                DefaultHeaders["Referer"] = Constants.SteamCommunityUrl + "/";
            }
        }

        public async Task<SteamMarketItemOrdersActivityJsonResponse> GetMarketItemOrdersActivityAsync(SteamMarketItemOrdersActivityJsonRequest request, string appId, string marketNameHash, bool useCache = false)
        {
            try
            {
                DefaultHeaders["Referer"] = new SteamMarketListingPageRequest()
                {
                    AppId = appId,
                    MarketHashName = marketNameHash,
                };

                return await GetJsonAsync<SteamMarketItemOrdersActivityJsonRequest, SteamMarketItemOrdersActivityJsonResponse>(request, useCache);
            }
            finally
            {
                DefaultHeaders["Referer"] = Constants.SteamCommunityUrl + "/";
            }
        }

        public async Task<SteamMarketItemOrdersHistogramJsonResponse> GetMarketItemOrdersHistogramAsync(SteamMarketItemOrdersHistogramJsonRequest request, string appId, string marketNameHash, bool useCache = false)
        {
            try
            {
                DefaultHeaders["Referer"] = new SteamMarketListingPageRequest()
                {
                    AppId = appId,
                    MarketHashName = marketNameHash,
                };

                return await GetJsonAsync<SteamMarketItemOrdersHistogramJsonRequest, SteamMarketItemOrdersHistogramJsonResponse>(request, useCache);
            }
            finally
            {
                DefaultHeaders["Referer"] = Constants.SteamCommunityUrl + "/";
            }
        }

        #endregion

        #region Workshop

        public async Task<XElement> GetWorkshopFileChangeNotesPageAsync(SteamWorkshopFileChangeNotesPageRequest request, bool useCache = false)
        {
            return await GetHtmlAsync<SteamWorkshopFileChangeNotesPageRequest>(request, useCache);
        }

        #endregion
    }
}
