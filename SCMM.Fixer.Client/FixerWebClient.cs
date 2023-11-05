using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Finance;
using SCMM.Shared.Web.Client;
using System.Text.Json;

namespace SCMM.Fixer.Client
{
    public class FixerWebClient : WebClientBase, ICurrencyExchangeService
    {
        private const string BaseUri = "https://data.fixer.io/api/";

        private readonly FixerConfiguration _configuration;

        public FixerWebClient(ILogger<FixerWebClient> logger, FixerConfiguration configuration) : base(logger)
        {
            _configuration = configuration;
        }

        public async Task<IDictionary<string, decimal>> GetHistoricalExchangeRatesAsync(DateTime date, string from, params string[] to)
        {
            using (var client = BuildWebApiHttpClient())
            {
                var url = $"{BaseUri}{date.ToString("yyyy-MM-dd")}?access_key={_configuration.ApiKey}&base={from}&symbols={string.Join(',', to)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<FixerHistoricalRatesResponseJson>(textJson);
                return responseJson?.Rates;
            }
        }
    }
}
