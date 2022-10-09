namespace SCMM.Shared.Abstractions.Analytics;

public interface IAnalysedImage
{
    string AccentColor { get; }

    IEnumerable<string> Colors { get; }

    IEnumerable<string> Tags { get; }

    IDictionary<string, double> Captions { get; }
}
