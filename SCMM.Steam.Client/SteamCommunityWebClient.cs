using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Community.Responses.Xml;
using System.Net;

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

        public async Task<SteamProfileXmlResponse> GetProfileById(SteamProfileByIdPageRequest request, bool useCache = true)
        {
            return await GetXml<SteamProfileByIdPageRequest, SteamProfileXmlResponse>(request, useCache);
        }

        #endregion

        #region Inventory

        public async Task<SteamInventoryPaginatedJsonResponse> GetInventoryPaginated(SteamInventoryPaginatedJsonRequest request, bool useCache = true)
        {
            try
            {
                DefaultHeaders["Referer"] = new SteamProfileInventoryPageRequest()
                {
                    AppId = request.AppId,
                    SteamId = request.SteamId,
                };

                return await GetJson<SteamInventoryPaginatedJsonRequest, SteamInventoryPaginatedJsonResponse>(request, useCache);
            }
            finally
            {
                DefaultHeaders["Referer"] = Constants.SteamCommunityUrl + "/";
            }
        }

        #endregion

        #region Market

        public async Task<SteamMarketSearchPaginatedJsonResponse> GetMarketSearchPaginated(SteamMarketSearchPaginatedJsonRequest request, bool useCache = false)
        {
            return await GetJson<SteamMarketSearchPaginatedJsonRequest, SteamMarketSearchPaginatedJsonResponse>(request, useCache);
        }

        public async Task<SteamMarketPriceOverviewJsonResponse> GetMarketPriceOverview(SteamMarketPriceOverviewJsonRequest request, bool useCache = false)
        {
            try
            {
                DefaultHeaders["Referer"] = new SteamMarketListingPageRequest()
                {
                    AppId = request.AppId,
                    MarketHashName = request.MarketHashName,
                };

                return await GetJson<SteamMarketPriceOverviewJsonRequest, SteamMarketPriceOverviewJsonResponse>(request, useCache);
            }
            finally
            {
                DefaultHeaders["Referer"] = Constants.SteamCommunityUrl + "/";
            }
        }

        public async Task<SteamMarketItemOrdersActivityJsonResponse> GetMarketItemOrdersActivity(SteamMarketItemOrdersActivityJsonRequest request, string appId, string marketNameHash, bool useCache = false)
        {
            try
            {
                DefaultHeaders["Referer"] = new SteamMarketListingPageRequest()
                {
                    AppId = appId,
                    MarketHashName = marketNameHash,
                };

                return await GetJson<SteamMarketItemOrdersActivityJsonRequest, SteamMarketItemOrdersActivityJsonResponse>(request, useCache);
            }
            finally
            {
                DefaultHeaders["Referer"] = Constants.SteamCommunityUrl + "/";
            }
        }

        public async Task<SteamMarketItemOrdersHistogramJsonResponse> GetMarketItemOrdersHistogram(SteamMarketItemOrdersHistogramJsonRequest request, string appId, string marketNameHash, bool useCache = false)
        {
            try
            {
                DefaultHeaders["Referer"] = new SteamMarketListingPageRequest()
                {
                    AppId = appId,
                    MarketHashName = marketNameHash,
                };

                return await GetJson<SteamMarketItemOrdersHistogramJsonRequest, SteamMarketItemOrdersHistogramJsonResponse>(request, useCache);
            }
            finally
            {
                DefaultHeaders["Referer"] = Constants.SteamCommunityUrl + "/";
            }
        }

        #endregion
    }
}
