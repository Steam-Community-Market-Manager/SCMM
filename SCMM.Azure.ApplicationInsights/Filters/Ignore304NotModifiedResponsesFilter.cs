using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Net;

namespace SCMM.Azure.ApplicationInsights.Filters;

public class Ignore304NotModifiedResponsesFilter : ITelemetryProcessor
{
    private ITelemetryProcessor Next { get; set; }

    public Ignore304NotModifiedResponsesFilter(ITelemetryProcessor next)
    {
        this.Next = next;
    }

    public void Process(ITelemetry item)
    {
        var request = item as RequestTelemetry;
        if (request != null && request.ResponseCode.Equals(((int)HttpStatusCode.NotModified).ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        this.Next.Process(item);
    }
}