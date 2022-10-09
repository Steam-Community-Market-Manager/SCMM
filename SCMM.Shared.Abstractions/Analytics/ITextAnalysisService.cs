namespace SCMM.Shared.Abstractions.Analytics;

public interface ITextAnalysisService
{
    Task<Sentiment> GetTextSentimentAsync(string text);
}
