using System.Net;
using System.Text.Json;

namespace SCMM.Market.RustSkins.Client
{
    public class RustSkinsWebClient : Shared.Client.WebClient
    {
        private const string BaseUri = "https://rustskins.com/api/v1/";

        public RustSkinsWebClient(IWebProxy webProxy) : base(webProxy: webProxy) { }

        public async Task<RustSkinsMarketListingsResponse> GetMarketListingsAsync(int page = 1)
        {
            using (var client = BuildWebBrowserHttpClient())
            {
                var url = $"{BaseUri}market/listings?sort=p-descending&page={page}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<RustSkinsMarketListingsResponse>(textJson);
                return responseJson;
            }
        }
    }
}
