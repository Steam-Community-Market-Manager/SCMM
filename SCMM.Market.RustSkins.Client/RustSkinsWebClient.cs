using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SCMM.Market.RustSkins.Client
{
    public class RustSkinsWebClient : Shared.Web.Client.WebClientBase
    {
        private const string ApiBaseUri = "https://api.rustskins.com/";

        public RustSkinsWebClient(ILogger<RustSkinsWebClient> logger) : base(logger) { }

        public async Task<IEnumerable<RustSkinsItem>> GetMarketplaceData()
        {
            using (var client = BuildWebApiHttpClient())
            {
                var url = $"{ApiBaseUri}external/marketplace/data";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<RustSkinsMarketListingsResponse>(textJson);
                return responseJson;
            }
        }
    }
}
