using SCMM.Market.Client;
using SCMM.Steam.Data.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SCMM.Market.DMarket.Client
{
    public class DMarketWebClient : MarketWebClient
    {
        private const string BaseUri = "https://api.dmarket.com/exchange/v1/";

        public const int MaxPageLimit = 100;

        public const string MarketTypeDMarket = "dmarket";
        public const string MarketTypeF2F = "p2p";

        public async Task<DMarketMarketItemsResponse> GetMarketItemsAsync(string appName, string marketType = MarketTypeDMarket, string currencyName = Constants.SteamCurrencyUSD, string cursor = null, int limit = MaxPageLimit)
        {
            const int rateLimitDelay = 3000;
            const int rateLimitMaxRetryCount = 3;
            var rateLimitRetryCount = 0;
            retry:

            try
            {
                using (var client = BuildHttpClient())
                {
                    var url = $"{BaseUri}market/items?side=market&orderBy=price&orderDir=desc&priceFrom=0&priceTo=0&treeFilters=&gameId={appName.ToLower()}&types={marketType}&cursor={cursor}&limit={limit}&currency={currencyName}&platform=browser&isLoggedIn=true";
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var textJson = await response.Content.ReadAsStringAsync();
                    var responseJson = JsonSerializer.Deserialize<DMarketMarketItemsResponse>(textJson);
                    return responseJson;
                }
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests && rateLimitRetryCount < rateLimitMaxRetryCount)
                {
                    Thread.Sleep(rateLimitDelay);
                    rateLimitRetryCount++;
                    goto retry;
                }
                throw;
            }
        }
    }
}
