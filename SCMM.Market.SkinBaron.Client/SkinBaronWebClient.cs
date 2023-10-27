using System.Net;
using System.Text.Json;

namespace SCMM.Market.SkinBaron.Client
{
    public class SkinBaronWebClient : Shared.Web.Client.WebClient
    {
        private const string ApiBaseUri = "https://skinbaron.de/api/v2/";

        public SkinBaronWebClient(IWebProxy webProxy) : base(webProxy: webProxy) { }

        public async Task<SkinBaronFilterOffersResponse> GetBrowsingFilterOffersAsync(string appId, int page = 1)
        {
            using (var client = BuildWebApiHttpClient())
            {
                var url = $"{ApiBaseUri}Browsing/FilterOffers?appId={Uri.EscapeDataString(appId)}&sort=EF&language=en&page={page}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<SkinBaronFilterOffersResponse>(textJson);
                return responseJson;
            }
        }
    }
}
