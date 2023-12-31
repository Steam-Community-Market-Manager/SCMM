﻿using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace SCMM.Market.CSDeals.Client
{
    public class CSDealsWebClient : Shared.Web.Client.WebClientBase
    {
        private const string WebsiteBaseUri = "https://cs.deals/";
        private const string ApiBaseUri = "https://cs.deals/API/";

        public CSDealsWebClient(ILogger<CSDealsWebClient> logger) : base(logger) { }

        public async Task<IEnumerable<CSDealsItemPrice>> GetPricingGetLowestPricesAsync(string appId)
        {
            using (var client = BuildWebApiHttpClient())
            {
                var url = $"{ApiBaseUri}IPricing/GetLowestPrices/v1?appid={Uri.EscapeDataString(appId)}";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<CSDealsResponse<CSDealsPricingGetLowestPricesResult>>(textJson);
                return responseJson?.Response?.Items;
            }
        }

        public async Task<CSDealsMarketplaceSearchResults<CSDealsItemListings>> PostMarketplaceSearchAsync(string appId, string appName, int page = 0)
        {
            using (var client = BuildWebBrowserHttpClient(referrer: new Uri($"{WebsiteBaseUri}/market/{appName?.ToLower()}")))
            {
                var url = $"{WebsiteBaseUri}ajax/marketplace-search";
                var payload = new FormUrlEncodedContent(new Dictionary<string, string>() {
                    { "appid", appId },
                    { "page", page.ToString() }
                });

                var response = await RetryPolicy.ExecuteAsync(() => client.PostAsync(url, payload));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<CSDealsResponse<CSDealsMarketplaceSearchResults<CSDealsItemListings>>>(textJson);
                return responseJson?.Response;
            }
        }

        public async Task<CSDealsBotsInventoryResult> PostBotsInventoryAsync(string appId)
        {
            using (var client = BuildWebBrowserHttpClient(referrer: new Uri($"{WebsiteBaseUri}/trade-skins")))
            {
                var url = $"{WebsiteBaseUri}ajax/botsinventory";
                var payload = new FormUrlEncodedContent(new Dictionary<string, string>() {
                    { "appid", appId }
                });

                var response = await RetryPolicy.ExecuteAsync(() => client.PostAsync(url, payload));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<CSDealsResponse<CSDealsBotsInventoryResult>>(textJson);
                return responseJson?.Response;
            }
        }
    }
}
