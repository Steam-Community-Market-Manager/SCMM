using System.Text.Json;

namespace SCMM.Market.SkinBaron.Client
{
    public class SkinBaronWebClient
    {
        private const string BaseUri = "https://skinbaron.de/api/v2/";

        public async Task<SkinBaronFilterOffersResponse> GetBrowsingFilterOffersAsync(string appId, int page = 1)
        {
            using (var client = new HttpClient())
            {
                var url = $"{BaseUri}Browsing/FilterOffers?appId={Uri.EscapeDataString(appId)}&sort=EF&language=en&page={page}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<SkinBaronFilterOffersResponse>(textJson);
                return responseJson;
            }
        }
    }
}
