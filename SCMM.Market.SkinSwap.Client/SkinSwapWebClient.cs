using System.Net;
using System.Text.Json;

namespace SCMM.Market.SkinSwap.Client
{
    public class SkinSwapWebClient : Shared.Client.WebClient
    {
        private const string WebBaseUri = "https://skinswap.com/";
        private const string ApiBaseUri = "https://skinswap.com/api/";

        public SkinSwapWebClient(IWebProxy webProxy) : base(webProxy: webProxy) { }

        public async Task<IEnumerable<SkinSwapItem>> GetInventoryAsync(string appId, int offset)
        {
            using (var client = BuildWebBrowserHttpClient(referer: new Uri(WebBaseUri)))
            {
                var url = $"{ApiBaseUri}site/inventory?appId={appId}&offset={offset}&sort=price-asc";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<SkinSwapResponse<SkinSwapItem[]>>(textJson);
                return responseJson?.Data;
            }
        }
    }
}
