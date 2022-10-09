using SCMM.Shared.Abstractions.Analytics;

namespace SCMM.Azure.AI;

public class AnalysedImage : IAnalysedImage
{
    public string AccentColor { get; set; }

    public IEnumerable<string> Colors { get; set; }

    public IEnumerable<string> Tags { get; set; }

    public IDictionary<string, double> Captions { get; set; }
}
