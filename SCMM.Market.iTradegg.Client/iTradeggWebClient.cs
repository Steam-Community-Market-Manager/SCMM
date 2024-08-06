using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SCMM.Market.iTradegg.Client
{
    public class iTradeggWebClient : Shared.Web.Client.WebClientBase
    {
        private const string ApiBaseUri = "https://api.itrade.gg/";

        private readonly iTradeggConfiguration _configuration;

        public iTradeggWebClient(ILogger<iTradeggWebClient> logger, iTradeggConfiguration configuration) : base(logger)
        {
            _configuration = configuration;
        }

        public async Task<iTradeggItems> GetInventoryAsync(string appName)
        {
            using (var client = BuildWebApiHttpClient())
            {
                var url = $"{ApiBaseUri}inventory/bot/{appName.ToLower()}/{_configuration.ApiKey}";
                var response = await RetryPolicy.ExecuteAsync(() => client.GetAsync(url));
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(textJson))
                {
                    return default;
                }

                var responseJson = JsonSerializer.Deserialize<iTradeggResponse<iTradeggItems>>(textJson);
                return responseJson?.Data;
            }
        }
    }
}
