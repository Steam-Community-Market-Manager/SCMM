using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace SCMM.Azure.ApplicationInsights.Filters;

public class IgnoreSyntheticRequestsFilter : ITelemetryProcessor
{
    private ITelemetryProcessor Next { get; set; }

    public IgnoreSyntheticRequestsFilter(ITelemetryProcessor next)
    {
        this.Next = next;
    }

    public void Process(ITelemetry item)
    {
        if (!string.IsNullOrEmpty(item.Context.Operation.SyntheticSource)) 
        { 
            return; 
        }

        this.Next.Process(item);
    }
}