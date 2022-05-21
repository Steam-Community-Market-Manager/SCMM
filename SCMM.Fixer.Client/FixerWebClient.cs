using System.Text.Json;

namespace SCMM.Fixer.Client
{
    public class FixerWebClient : Worker.Client.WebClient
    {
        private const string BaseUri = "https://data.fixer.io/api/";

        private readonly FixerConfiguration _configuration;

        public FixerWebClient(FixerConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IDictionary<string, decimal>> GetHistoricalRatesAsync(DateTime date, string from, params string[] to)
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
