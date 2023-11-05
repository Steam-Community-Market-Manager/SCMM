using Microsoft.Extensions.Logging;
using SCMM.Steam.Data.Models;
using System.Net;
using System.Text.Json;

namespace SCMM.Market.DMarket.Client
{
    public class DMarketWebClient : Shared.Web.Client.WebClientBase
    {
        private const string ApiBaseUri = "https://api.dmarket.com/";

        public const int MaxPageLimit = 100;

        public const string MarketTypeDMarket = "dmarket";
        public const string MarketTypeF2F = "p2p";

        public DMarketWebClient(ILogger<DMarketWebClient> logger, IWebProxy webProxy) : base(logger, webProxy: webProxy) { }

        /// <see cref="https://docs.dmarket.com/v1/swagger.html#/Sell%20Items/getMarketItems"/>
        /// <seealso cref="https://dmarket.com/faq#rateLimits"/>
        public async Task<DMarketMarketItemsResponse> GetMarketItemsAsync(string appName, string marketType = MarketTypeDMarket, string currencyName = Constants.SteamCurrencyUSD, string cursor = null, int limit = MaxPageLimit)
        {
            using (var client = BuildWebApiHttpClient())
            {
                var url = $"{ApiBaseUri}exchange/v1/market/items?side=market&orderBy=price&orderDir=desc&priceFrom=0&priceTo=0&treeFilters=&gameId={appName.ToLower()}&types={marketType}&cursor={cursor}&limit={limit}&currency={currencyName}&platform=browser&isLoggedIn=true";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<DMarketMarketItemsResponse>(textJson);
                return responseJson;
            }
        }
    }
}
