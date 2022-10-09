using Azure;
using Azure.AI.AnomalyDetector;
using Azure.AI.AnomalyDetector.Models;
using Azure.AI.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using SCMM.Shared.Abstractions.Analytics;

namespace SCMM.Azure.AI
{
    public class AzureAiClient : ITimeSeriesAnalysisService, IImageAnalysisService, ITextAnalysisService
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

        public async Task<IEnumerable<ITimeSeriesAnomaly>> DetectTimeSeriesAnomaliesAsync(IDictionary<DateTimeOffset, float> data, Shared.Abstractions.Analytics.TimeGranularity? granularity = Shared.Abstractions.Analytics.TimeGranularity.Daily, int? sensitivity = null)
        {
            if (_anomalyDetectorClient == null)
            {
                throw new InvalidOperationException("Anomaly detector is not configured");
            }

            var dataPoints = data
                .Select(x => new TimeSeriesPoint(x.Value)
                {
                    Timestamp = x.Key
                })
                .OrderBy(x => x.Timestamp)
                .ToArray();

            var anomalies = new List<TimeSeriesAnomaly>();
            var response = (EntireDetectResponse)await _anomalyDetectorClient.DetectEntireSeriesAsync(new DetectRequest(dataPoints)
            {
                Granularity = (global::Azure.AI.AnomalyDetector.Models.TimeGranularity)granularity,
                Sensitivity = sensitivity
            });

            for (var i = 0; i < dataPoints.Length; ++i)
            {
                if (response.IsAnomaly[i])
                {
                    anomalies.Add(new TimeSeriesAnomaly()
                    {
                        Timestamp = dataPoints[i].Timestamp,
                        ActualValue = dataPoints[i].Value,
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
