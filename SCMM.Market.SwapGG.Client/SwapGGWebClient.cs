using System.Text.Json;

namespace SCMM.Market.SwapGG.Client
{
    public class SwapGGWebClient
    {
        private const string BaseUri = "https://market-api.swap.gg/v1/";

        public async Task<IDictionary<string, SwapGGItemPrice>> GetItemPricingLowestAsync(string appId)
        {
            using (var client = new HttpClient())
            {
                var url = $"{BaseUri}/pricing/lowest?appId={Uri.EscapeDataString(appId)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<SwapGGResponse<IDictionary<string, SwapGGItemPrice>>>(textJson);
                return responseJson?.Result;
            }
        }
    }
}
