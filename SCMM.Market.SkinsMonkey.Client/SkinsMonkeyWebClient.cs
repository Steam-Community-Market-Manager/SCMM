﻿using System.Net;
using System.Text.Json;

namespace SCMM.Market.SkinsMonkey.Client
{
    public class SkinsMonkeyWebClient : Shared.Web.Client.WebClient
    {
        private const string ApiBaseUri = "https://skinsmonkey.com/api/public/v1/";

        private readonly SkinsMonkeyConfiguration _configuration;

        public SkinsMonkeyWebClient(SkinsMonkeyConfiguration configuration, IWebProxy webProxy) : base(webProxy: webProxy)
        {
            _configuration = configuration;
        }

        public async Task<IEnumerable<SkinsMonkeyItem>> GetItemPricesAsync(string appId)
        {
            using (var client = BuildSkinsMoneyClient())
            {
                var url = $"{ApiBaseUri}price/{Uri.EscapeDataString(appId)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<SkinsMonkeyItem>>(textJson);
            }
        }

        private HttpClient BuildSkinsMoneyClient() => BuildWebApiHttpClient(
            authHeaderName: "x-api-key",
            authHeaderFormat: "{0}",
            authKey: _configuration.ApiKey
        );
    }
}
