using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using SCMM.Shared.Abstractions.Analytics;

namespace SCMM.Azure.AI
{
    public class AzureAiClient : IImageAnalysisService, ITextAnalysisService
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

        public async Task<IAnalysedImage> AnalyseImageAsync(Stream image)
        {
            if (_computerVisionClient == null)
            {
                throw new InvalidOperationException("Computer vision is not configured");
            }

            var result = await _computerVisionClient.AnalyzeImageInStreamAsync(
                image,
                visualFeatures: new List<VisualFeatureTypes?> {
                    VisualFeatureTypes.Color, VisualFeatureTypes.Tags, VisualFeatureTypes.Description
                }
            );

            return new AnalysedImage
            {
                AccentColor = result?.Color?.AccentColor,
                Colors = result?.Color?.DominantColors,
                Tags = result?.Description?.Tags,
                Captions = result?.Description?.Captions?.ToDictionary(x => x.Text, x => x.Confidence)
            };
        }

        public async Task<Sentiment> GetTextSentimentAsync(string text)
        {
            if (_textAnalyticsClient == null)
            {
                throw new InvalidOperationException("Text analytics is not configured");
            }

            var response = await _textAnalyticsClient.AnalyzeSentimentAsync(text);
            return (Sentiment)(response?.Value?.Sentiment ?? TextSentiment.Neutral);
        }
    }
}
