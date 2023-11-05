using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace SCMM.Market.Skinport.Client
{
    /// <remarks>
    /// No Authorization required.
    /// Rate Limit 8 requests / 5 Minutes.
    /// Endpoint is cached by 5 minutes.
    /// </remarks>
    /// <see cref="https://docs.skinport.com"/>
    public class SkinportWebClient : Shared.Web.Client.WebClientBase
    {
        private const string BaseUri = "https://api.skinport.com/v1/";

        public SkinportWebClient(ILogger<SkinportWebClient> logger, IWebProxy webProxy) : base(logger, webProxy: webProxy)
        {
        }

        /// <see cref="https://docs.skinport.com/#items"/>
        public async Task<IEnumerable<SkinportItem>> GetItemsAsync(string appId, string currency = null, bool tradable = false)
        {
            using (var client = BuildWebApiHttpClient(host: new Uri(BaseUri)))
            {
                var url = $"{BaseUri}items?app_id={Uri.EscapeDataString(appId)}&currency={Uri.EscapeDataString(currency)}&tradable={tradable.ToString().ToLower()}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<SkinportItem[]>(textJson);
                return responseJson;
            }
        }
    }
}
