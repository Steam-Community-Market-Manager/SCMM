using System.Text.Json;

namespace SCMM.Fixer.Client
{
    public class FixerWebClient : IDisposable
    {
        private const string BaseUri = "https://data.fixer.io/api/";

        private readonly FixerConfiguration _cfg;
        private readonly HttpClientHandler _httpHandler;
        private bool _disposedValue;

        public FixerWebClient(FixerConfiguration cfg)
        {
            _cfg = cfg;
            _httpHandler = new HttpClientHandler();
        }

        public async Task<IDictionary<string, decimal>> GetHistoricalRatesAsync(DateTime date, string from, params string[] to)
        {
            using (var client = new HttpClient(_httpHandler, false))
            {
                var url = $"{BaseUri}{date.ToString("yyyy-MM-dd")}?access_key={_cfg.ApiKey}&base={from}&symbols={string.Join(',', to)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<FixerHistoricalRatesResponseJson>(textJson);
                return responseJson?.Rates;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _httpHandler.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
