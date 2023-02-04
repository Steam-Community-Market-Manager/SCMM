using System.Net;
using System.Text.Json;

namespace SCMM.Market.Skinport.Client
{
    public class SkinportWebClient : Shared.Client.WebClient
    {
        private const string BaseUri = "https://api.skinport.com/v1/";

        public SkinportWebClient(IWebProxy webProxy) : base(webProxy: webProxy) { }

        /// <summary>
        /// 
        /// </summary>
        /// <see cref="https://docs.skinport.com/#items"/>
        /// <remarks>
        /// No Authorization required.
        /// Rate Limit 8 requests / 5 Minutes.
        /// Endpoint is cached by 5 minutes.
        /// </remarks>
        /// <param name="appId"></param>
        /// <param name="currency"></param>
        /// <param name="tradable"></param>
        /// <returns></returns>
        public async Task<IEnumerable<SkinportItem>> GetItemsAsync(string appId, string currency = null, bool tradable = false)
        {
            using (var client = BuildWebApiHttpClient())
            {
                var url = $"{BaseUri}items?app_id={Uri.EscapeDataString(appId)}&currency={Uri.EscapeDataString(currency)}&tradable={tradable.ToString().ToLower()}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<SkinportItem[]>(textJson);
                return responseJson;
            }
        }
    }
}
