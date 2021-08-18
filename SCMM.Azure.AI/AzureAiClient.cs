using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace SCMM.Azure.AI
{
    public class AzureAiClient
    {
        private readonly ComputerVisionClient _computerVisionClient;
        private readonly TextAnalyticsClient _textAnalyticsClient;

        public AzureAiClient(AzureAiConfiguration config)
        {
            if (config.ComputerVision != null)
            {
                _computerVisionClient = new ComputerVisionClient(new ApiKeyServiceClientCredentials(config.ComputerVision.ApiKey))
                {
                    Endpoint = config.ComputerVision.Endpoint
                };
            }
            if (config.TextAnalytics != null)
            {
                _textAnalyticsClient = new TextAnalyticsClient(new Uri(config.TextAnalytics.Endpoint), new AzureKeyCredential(config.TextAnalytics.ApiKey));
            }
        }

        public async Task<ImageAnalysis> AnalyseImageAsync(Stream image, params VisualFeatureTypes?[] visualFeatures)
        {
            if (_computerVisionClient == null)
            {
                throw new InvalidOperationException("Computer vision is not configured");
            }

            return await _computerVisionClient.AnalyzeImageInStreamAsync(
                image,
                visualFeatures: visualFeatures?.ToList()
            );
        }

        public async Task<TextSentiment> GetTextSentimentAsync(string text)
        {
            if (_textAnalyticsClient == null)
            {
                throw new InvalidOperationException("Text analytics is not configured");
            }

            var response = await _textAnalyticsClient.AnalyzeSentimentAsync(text);
            return response?.Value?.Sentiment ?? TextSentiment.Neutral;
        }
    }
}
