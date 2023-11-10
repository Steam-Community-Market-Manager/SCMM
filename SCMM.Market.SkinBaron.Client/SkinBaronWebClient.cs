using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace SCMM.Market.SkinBaron.Client
{
    /// <see cref="https://skinbaron.de/misc/apidoc/"/>
    public class SkinBaronWebClient : Shared.Web.Client.WebClientBase
    {
        private const string ApiBaseUri = "https://skinbaron.de/api/v2/";

        public SkinBaronWebClient(ILogger<SkinBaronWebClient> logger) : base(logger) { }

        /// <remarks>
        /// This is currently broken as SkinBaron have changed their APIs and this one is no longer supported
        /// </remarks>
        public async Task<SkinBaronFilterOffersResponse> GetBrowsingFilterOffersAsync(string appId, int page = 1)
        {
            using (var client = BuildWebApiHttpClient())
            {
                var url = $"{ApiBaseUri}Browsing/FilterOffers?appId={Uri.EscapeDataString(appId)}&sort=EF&language=en&page={page}";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<SkinBaronFilterOffersResponse>(textJson);
                return responseJson;
            }
        }
    }
}
