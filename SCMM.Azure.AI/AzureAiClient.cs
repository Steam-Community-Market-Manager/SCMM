using Azure;
using Azure.AI.AnomalyDetector;
using Azure.AI.AnomalyDetector.Models;
using Azure.AI.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace SCMM.Azure.AI
{
    public class AzureAiClient
    {
        private readonly AnomalyDetectorClient _anomalyDetectorClient;
        private readonly ComputerVisionClient _computerVisionClient;
        private readonly TextAnalyticsClient _textAnalyticsClient;

        public AzureAiClient(AzureAiConfiguration config)
        {
            if (config.AnomalyDetector != null)
            {
                _anomalyDetectorClient = new AnomalyDetectorClient(new Uri(config.AnomalyDetector.Endpoint), new AzureKeyCredential(config.AnomalyDetector.ApiKey));
            }
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

        public async Task<IEnumerable<TimeSeriesAnomaly>> DetectTimeSeriesAnomaliesAsync(IEnumerable<TimeSeriesPoint> data, TimeGranularity? granularity = TimeGranularity.Daily, int? sensitivity = null)
        {
            if (_anomalyDetectorClient == null)
            {
                throw new InvalidOperationException("Anomaly detector is not configured");
            }

            var rawData = data.OrderBy(x => x.Timestamp).ToArray();
            var anomalies = new List<TimeSeriesAnomaly>();
            var response = (EntireDetectResponse) await _anomalyDetectorClient.DetectEntireSeriesAsync(new DetectRequest(rawData)
            {
                Granularity = granularity,
                Sensitivity = sensitivity
            });

            for (var i = 0; i < rawData.Length; ++i)
            {
                if (response.IsAnomaly[i])
                {
                    anomalies.Add(new TimeSeriesAnomaly()
                    {
                        Timestamp = rawData[i].Timestamp,
                        ActualValue = rawData[i].Value,
                        ExpectedValue = response.ExpectedValues[i],
                        UpperMargin = response.ExpectedValues[i],
                        LowerMargin = response.ExpectedValues[i],
                        IsNegative = response.IsNegativeAnomaly[i],
                        IsPositive = response.IsPositiveAnomaly[i],
                        Severity = response.Severity[i]
                    });
                }
            }

            return anomalies;
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
