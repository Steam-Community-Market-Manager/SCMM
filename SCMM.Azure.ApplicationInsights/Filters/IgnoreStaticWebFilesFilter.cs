﻿using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace SCMM.Azure.ApplicationInsights.Filters;

public class IgnoreStaticWebFilesFilter : ITelemetryProcessor
{
    private ITelemetryProcessor Next { get; set; }

    public IgnoreStaticWebFilesFilter(ITelemetryProcessor next)
    {
        this.Next = next;
    }

    public void Process(ITelemetry item)
    {
        if (item is RequestTelemetry request)
        {
            if (request.Url.AbsolutePath.StartsWith("/_Host") || 
                request.Url.AbsolutePath.StartsWith("/_framework/") ||
                request.Url.AbsolutePath.StartsWith("/_content/") ||
                request.Url.AbsolutePath.StartsWith("/css/") ||
                request.Url.AbsolutePath.StartsWith("/js/") ||
                request.Url.AbsolutePath.StartsWith("/images/") ||
                request.Url.AbsolutePath.EndsWith(".ico") ||
                request.Url.AbsolutePath.EndsWith(".svg") ||
                request.Url.AbsolutePath.EndsWith(".png") ||
                request.Url.AbsolutePath.EndsWith(".txt") ||
                request.Url.AbsolutePath.EndsWith(".css") ||
                request.Url.AbsolutePath.EndsWith(".js") ||
                request.Url.AbsolutePath.EndsWith(".gz"))
            {
                return;
            }
        }

        this.Next.Process(item);
    }
}