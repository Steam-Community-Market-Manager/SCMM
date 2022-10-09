namespace SCMM.Shared.Abstractions.Analytics;

public interface IImageAnalysisService
{
    Task<IAnalysedImage> AnalyseImageAsync(Stream image);
}
