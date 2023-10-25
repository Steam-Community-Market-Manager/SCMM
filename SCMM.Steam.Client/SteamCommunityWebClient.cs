﻿using Microsoft.Extensions.Caching.Distributed;
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
    /// Client for https://steamcommunity.com/.
    /// Most requests can be done anonymous, some require a valid Steam session cookie.
    /// </summary>
    /// <remarks>
    /// Steam community web site rate-limits observed from personal testing:
    ///  - You are limited to 25 requests within 30 seconds, which resets after ???.
    /// </remarks>
    public class SteamCommunityWebClient : SteamWebClient
    {
        public SteamCommunityWebClient(ILogger<SteamCommunityWebClient> logger, IDistributedCache cache)
            : base(logger, cache)
        {
        }
    }

    /// <inheritdoc />
    public class ProxiedSteamCommunityWebClient : SteamWebClient
    {
        public ProxiedSteamCommunityWebClient(ILogger<SteamCommunityWebClient> logger, IDistributedCache cache, IWebProxy proxy)
            : base(logger, cache, proxy: proxy)
        {
            // Transport
            DefaultHeaders.Add("Host", new Uri(Constants.SteamCommunityUrl).Host);
            DefaultHeaders.Add("Referer", Constants.SteamCommunityUrl + "/");
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
