using Azure;
using Azure.AI.TextAnalytics;
using System;
using System.Threading.Tasks;

namespace SCMM.Azure.AI
{
    public class AzureAiClient
    {
        private readonly TextAnalyticsClient _textAnalyticsClient;

        public AzureAiClient(AzureAiConfiguration config)
        {
            _textAnalyticsClient = new TextAnalyticsClient(new Uri(config.Endpoint), new AzureKeyCredential(config.ApiKey));
        }

        public async Task<TextSentiment> GetTextSentimentAsync(string text)
        {
            var response = await _textAnalyticsClient.AnalyzeSentimentAsync(text);
            return response?.Value?.Sentiment ?? TextSentiment.Neutral;
        }
    }
}
