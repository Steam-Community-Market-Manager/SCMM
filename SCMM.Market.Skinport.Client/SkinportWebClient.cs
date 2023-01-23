using System.Net;
using System.Text.Json;

namespace SCMM.Market.Skinport.Client
{
    public class SkinportWebClient : Shared.Client.WebClient
    {
        private const string BaseUri = "https://api.skinport.com/v1/";

        public SkinportWebClient(IWebProxy webProxy) : base(webProxy: webProxy) { }

        public async Task<IEnumerable<SkinportItem>> GetItemsAsync(string appId, string currency = null)
        {
            using (var client = BuildWebBrowserHttpClient())
            {
                var url = $"{BaseUri}items?app_id={Uri.EscapeDataString(appId)}&currency={Uri.EscapeDataString(currency)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<SkinportItem[]>(textJson);
                return responseJson;
            }
        }
    }
}
