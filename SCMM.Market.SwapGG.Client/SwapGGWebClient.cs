using System.Net;
using System.Text.Json;

namespace SCMM.Market.SwapGG.Client
{
    public class SwapGGWebClient : Shared.Web.Client.WebClient
    {
        private const string WebsiteBaseUri = "https://swap.gg/";
        private const string TradeApiBaseUri = "https://api.swap.gg/";
        private const string MarketApiBaseUri = "https://market-api.swap.gg/v1/";

        public SwapGGWebClient(IWebProxy webProxy) : base(webProxy: webProxy) { }

        /// <summary>
        /// 
        /// </summary>
        /// <see cref="https://swap.gg/"/>
        /// <param name="appId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<SwapGGTradeItem>> GetTradeBotInventoryAsync(string appId)
        {
            using (var client = BuildWebApiHttpClient(host: new Uri(TradeApiBaseUri)))
            {
                var url = $"{TradeApiBaseUri}inventory/bot/{Uri.EscapeDataString(appId)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<SwapGGResponse<IEnumerable<SwapGGTradeItem>>>(textJson);
                return responseJson?.Result;
            }
        }

        /// <summary>
        /// This call will return a list of items with their lowest price on market.swap.gg.
        /// </summary>
        /// <remarks>
        /// This request is cached for about 30 minutes.
        /// </remarks>
        /// <see cref="https://docs.swap.gg/#get-lowest-list-prices"/>
        /// <param name="appId"></param>
        /// <returns></returns>
        public async Task<IDictionary<string, SwapGGMarketItem>> GetMarketPricingLowestAsync(string appId)
        {
            using (var client = BuildWebApiHttpClient())
            {
                var url = $"{MarketApiBaseUri}pricing/lowest?appId={Uri.EscapeDataString(appId)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<SwapGGResponse<IDictionary<string, SwapGGMarketItem>>>(textJson);
                return responseJson?.Result;
            }
        }
    }
}
